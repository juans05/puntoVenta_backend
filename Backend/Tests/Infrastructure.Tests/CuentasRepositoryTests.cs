using Application.Abstractions;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class CuentasRepositoryTests
{
    private static (CuentasRepository Cuentas, SpaContext Context, System.Data.Common.DbConnection Connection) Preparar(string? username = null)
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        Microsoft.AspNetCore.Http.IHttpContextAccessor? accessor = username == null ? null : new FakeHttpContextAccessor(username);
        return (new CuentasRepository(context, accessor), context, connection);
    }

    private static async Task<int> SeedMetodoPago(SpaContext context, int id = 1)
    {
        context.Metodopago.Add(new Metodopago { Id = id, Nombre = id == 1 ? "Efectivo" : "Transferencia" });
        await context.SaveChangesAsync();
        return id;
    }

    // Venta a credito ya emitida: cabecera con EsCredito y SIN Pago inicial.
    private static async Task<(int ClienteId, int ComprobanteId)> SeedVentaCredito(SpaContext context, decimal total, DateTime? vencimiento = null)
    {
        var cliente = new Cliente { Nombre = "CLIENTE SAC", NumeroDocumento = "20123456789" };
        context.Cliente.Add(cliente);
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = (int)TipoComprobante.Factura, Nombre = "FACTURA" });
        await context.SaveChangesAsync();
        var venta = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = (int)TipoComprobante.Factura, ClienteId = cliente.Id, Serie = "F001", Correlativo = 1,
            ValorTotal = total, EsCredito = true, FechaVenta = DateTime.UtcNow.AddHours(-5).AddDays(-40), FechaVencimiento = vencimiento
        };
        context.ComprobanteCabecera.Add(venta);
        await context.SaveChangesAsync();
        return (cliente.Id, venta.Id);
    }

    private static async Task<(int ProveedorId, int CompraId)> SeedCompraCredito(SpaContext context, decimal total)
    {
        var proveedor = new Proveedor { Nombre = "PROVEEDOR SAC", Ruc = "20999999999" };
        context.Proveedor.Add(proveedor);
        await context.SaveChangesAsync();
        var compra = new Compra
        {
            NumeroCompra = "C-000001", ProveedorId = proveedor.Id, Total = total, EsCredito = true, Estado = "CONFIRMADO",
            FechaCompra = DateTime.UtcNow.AddHours(-5).AddDays(-10), Serie = "F001", Numero = "55"
        };
        context.Compra.Add(compra);
        await context.SaveChangesAsync();
        return (proveedor.Id, compra.Id);
    }

    private static RegistrarPagoCuentaPayload Pago(int socioId, int metodoId, int documentoId, decimal monto) => new()
    {
        SocioId = socioId, MetodoPagoId = metodoId,
        Detalle = new() { new() { DocumentoId = documentoId, Monto = monto } }
    };

    [Fact]
    public async Task VentaACredito_AparecePorCobrarConSuSaldoCompleto()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var (clienteId, _) = await SeedVentaCredito(context, 100m);

        var (_, resumen, _) = await repo.Resumen(cobrar: true);

        var socio = Assert.Single(resumen!);
        Assert.Equal(clienteId, socio.Id);
        Assert.Equal(100m, socio.Saldo);
        Assert.Equal(100m, socio.Vencido); // 40 dias de antiguedad y sin vencimiento propio
    }

    [Fact]
    public async Task VentaAlContado_NoGeneraCuentaPorCobrar()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var (_, ventaId) = await SeedVentaCredito(context, 100m);
        var venta = await context.ComprobanteCabecera.AsTracking().FirstAsync(v => v.Id == ventaId);
        venta.EsCredito = false;
        await context.SaveChangesAsync();

        var (_, resumen, _) = await repo.Resumen(cobrar: true);

        Assert.Empty(resumen!);
    }

    [Fact]
    public async Task CobroParcial_ReduceElSaldoYCreaUnPagoPorDocumento()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var metodo = await SeedMetodoPago(context, 2);
        var (clienteId, ventaId) = await SeedVentaCredito(context, 100m);

        var (estado, cobro, mensaje) = await repo.RegistrarPago(true, Pago(clienteId, metodo, ventaId, 30m));

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(30m, cobro!.Monto);
        var (_, docs, _) = await repo.Documentos(true, clienteId);
        Assert.Equal(70m, Assert.Single(docs!).Saldo);
        var pago = await context.Pago.AsNoTracking().SingleAsync();
        Assert.Equal((ventaId, 30m), (pago.ComprobanteCabeceraId, pago.Monto));
    }

    [Fact]
    public async Task Cobro_NoPuedeSuperarElSaldo()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var metodo = await SeedMetodoPago(context, 2);
        var (clienteId, ventaId) = await SeedVentaCredito(context, 100m);

        var (estado, _, mensaje) = await repo.RegistrarPago(true, Pago(clienteId, metodo, ventaId, 150m));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("supera el saldo", mensaje);
        Assert.Empty(await context.Pago.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CobroTotal_LiquidaElDocumento()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var metodo = await SeedMetodoPago(context, 2);
        var (clienteId, ventaId) = await SeedVentaCredito(context, 100m);

        await repo.RegistrarPago(true, Pago(clienteId, metodo, ventaId, 100m));

        var (_, resumen, _) = await repo.Resumen(cobrar: true);
        Assert.Empty(resumen!);
    }

    [Fact]
    public async Task AnularCobro_ReabreLaDeudaConUnPagoNegativo()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var metodo = await SeedMetodoPago(context, 2);
        var (clienteId, ventaId) = await SeedVentaCredito(context, 100m);
        var (_, cobro, _) = await repo.RegistrarPago(true, Pago(clienteId, metodo, ventaId, 100m));

        var (estado, anulado, _) = await repo.AnularPago(true, cobro!.Id);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(EstadoPagoCuenta.Anulado, anulado!.Estado);
        var (_, docs, _) = await repo.Documentos(true, clienteId);
        Assert.Equal(100m, Assert.Single(docs!).Saldo);
        Assert.Equal(0m, (await context.Pago.AsNoTracking().ToListAsync()).Sum(p => p.Monto)); // +100 y -100
    }

    [Fact]
    public async Task Antiguedad_ClasificaPorTramosSegunVencimiento()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        // Vence hace 45 dias -> tramo 31-60.
        var (_, _) = await SeedVentaCredito(context, 80m, vencimiento: DateTime.UtcNow.AddHours(-5).AddDays(-45));

        var (_, aging, _) = await repo.Antiguedad(cobrar: true);

        var fila = Assert.Single(aging!);
        Assert.Equal(80m, fila.Dias31a60);
        Assert.Equal(0m, fila.PorVencer + fila.Dias1a30 + fila.Dias61a90 + fila.Mas90);
    }

    [Fact]
    public async Task PagoProveedor_Transferencia_ReduceElSaldoDeLaCompra()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var metodo = await SeedMetodoPago(context, 2);
        var (proveedorId, compraId) = await SeedCompraCredito(context, 200m);

        var (estado, _, mensaje) = await repo.RegistrarPago(false, Pago(proveedorId, metodo, compraId, 50m));

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        var (_, docs, _) = await repo.Documentos(false, proveedorId);
        Assert.Equal(150m, Assert.Single(docs!).Saldo);
    }

    [Fact]
    public async Task PagoProveedor_Efectivo_SinCajaAbierta_EsRechazado()
    {
        var (repo, context, connection) = Preparar("cajero");
        using var _ = connection; using var __ = context;
        var efectivo = await SeedMetodoPago(context, 1);
        var (proveedorId, compraId) = await SeedCompraCredito(context, 200m);

        var (estado, _, mensaje) = await repo.RegistrarPago(false, Pago(proveedorId, efectivo, compraId, 50m));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("caja", mensaje);
    }

    [Fact]
    public async Task PagoProveedor_Efectivo_ConCajaAbierta_CreaRetiroYAnularloLoRevierte()
    {
        var (repo, context, connection) = Preparar("CAJERO");
        using var _ = connection; using var __ = context;
        var efectivo = await SeedMetodoPago(context, 1);
        var (proveedorId, compraId) = await SeedCompraCredito(context, 200m);
        // Caja abierta hoy por el usuario (UsuarioCreacion la fija SaveChanges desde el tenant de prueba).
        var caja = new Caja { MontoInicio = 100 };
        context.Caja.Add(caja);
        await context.SaveChangesAsync();
        var trackedCaja = await context.Caja.AsTracking().FirstAsync();
        trackedCaja.UsuarioCreacion = "CAJERO";
        trackedCaja.FechaCreacion = DateTime.UtcNow.AddHours(-5);
        await context.SaveChangesAsync();

        var (estado, pago, mensaje) = await repo.RegistrarPago(false, Pago(proveedorId, efectivo, compraId, 50m));
        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(50m, (await context.Retiros.AsNoTracking().SingleAsync()).Monto);

        var (anular, _, _) = await repo.AnularPago(false, pago!.Id);
        Assert.Equal(ServiceStatus.Ok, anular);
        Assert.Equal(0m, (await context.Retiros.AsNoTracking().ToListAsync()).Sum(r => r.Monto)); // +50 y -50
        var (_, docs, _) = await repo.Documentos(false, proveedorId);
        Assert.Equal(200m, Assert.Single(docs!).Saldo);
    }

    [Fact]
    public async Task VentaACredito_NoCreaPagoInicialYExigeCliente()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection; using var __ = context;
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 2, Nombre = "BOLETA" });
        var producto = new Producto { Nombre = "P", Precio = 10, Stock = 10, RestriccionEdad = 0 };
        context.Producto.Add(producto);
        context.Metodopago.Add(new Metodopago { Id = 1, Nombre = "Efectivo" });
        await context.SaveChangesAsync();
        var comprobante = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new TaxCalculatorFactory());

        ComprobantePayload Payload(bool credito, string? doc) => new()
        {
            TipoDocumentoVentaId = 2, EsCredito = credito, NumeroDocumento = doc, RazonSocial = doc == null ? null : "CLIENTE",
            Total = 10m,
            DetalleComprobante = new() { new() { ProductoId = producto.Id, Cantidad = 1, ValorUnitario = 10m } },
            DetallePago = new() { new() { MetodoPagoId = 1, Monto = 10m } }
        };

        var contado = await comprobante.CrearComprobante(Payload(false, null));
        Assert.Equal(ServiceStatus.Ok, contado.Item1);
        Assert.Single(await context.Pago.AsNoTracking().ToListAsync()); // el contado si crea su Pago

        var cliente = new Cliente { Nombre = "CLIENTE SAC", NumeroDocumento = "20123456789" };
        context.Cliente.Add(cliente);
        await context.SaveChangesAsync();
        var credito = Payload(true, null);
        credito.ClienteId = cliente.Id;
        var (estadoCredito, _, msgCredito) = await comprobante.CrearComprobante(credito);
        Assert.True(estadoCredito == ServiceStatus.Ok, msgCredito);
        Assert.Single(await context.Pago.AsNoTracking().ToListAsync()); // el credito NO agrego otro Pago

        // Al final: un rechazo deja la transaccion abierta en esta conexion de prueba.
        var (sinCliente, _, mensaje) = await comprobante.CrearComprobante(Payload(true, null));
        Assert.Equal(ServiceStatus.FailedValidation, sinCliente);
        Assert.Contains("cliente", mensaje);
    }
}
