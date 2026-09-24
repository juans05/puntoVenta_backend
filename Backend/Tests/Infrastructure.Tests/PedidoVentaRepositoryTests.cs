using Application.Abstractions;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class PedidoVentaRepositoryTests
{
    private static (PedidoVentaRepository Repo, ComprobanteRepository Comprobante, OrdenCompraRepository Config, SpaContext Context, System.Data.Common.DbConnection Connection) Preparar()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        var repo = new PedidoVentaRepository(context, httpContextAccessor: null);
        var comprobante = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new TaxCalculatorFactory());
        var compra = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);
        var config = new OrdenCompraRepository(context, compra, httpContextAccessor: null);
        return (repo, comprobante, config, context, connection);
    }

    private static Task ConfigurarVentas(OrdenCompraRepository config, string flujo)
        => config.GuardarConfiguracion(new ConfiguracionFlujoPayload
        {
            FlujoCompras = FlujoComprasModo.Simplificado,
            CruceFactura = CruceFacturaModo.Advertir,
            FlujoVentas = flujo
        });

    private static async Task<int> SeedProductoAsync(SpaContext context, int stock)
    {
        var p = new Producto { Nombre = "Producto PV", Precio = 10, Stock = stock, RestriccionEdad = 0 };
        context.Producto.Add(p);
        await context.SaveChangesAsync();
        return p.Id;
    }

    private static CreatePedidoVentaPayload Pedido(int productoId, int cantidad, decimal precio = 10m, bool borrador = false) => new()
    {
        NumeroDocumento = "20123456789",
        RazonSocial = "CLIENTE SAC",
        Borrador = borrador,
        Detalle = new() { new() { ProductoId = productoId, Cantidad = cantidad, ValorUnitario = precio } }
    };

    private static async Task<int> StockAsync(SpaContext context, int productoId)
        => (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock ?? 0;

    [Fact]
    public async Task ModoSimplificado_NoPermiteCrearPedidos()
    {
        var (repo, _, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var productoId = await SeedProductoAsync(context, 10);

        var (estado, _, mensaje) = await repo.Crear(Pedido(productoId, 1));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("no está activo", mensaje);
    }

    [Fact]
    public async Task Confirmar_ReservaStock_ElSegundoPedidoNoPuedeUsarLoReservado()
    {
        var (repo, _, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Completo);
        var productoId = await SeedProductoAsync(context, 10);

        var (e1, primero, _) = await repo.Crear(Pedido(productoId, 8));
        Assert.Equal(ServiceStatus.Ok, e1);
        Assert.Equal(EstadosPedidoVenta.Confirmado, primero!.EstadoPedidoVenta);
        Assert.Equal(10, await StockAsync(context, productoId)); // reservar no baja el stock real

        var (e2, _, mensaje) = await repo.Crear(Pedido(productoId, 5)); // solo quedan 2 libres
        Assert.Equal(ServiceStatus.FailedValidation, e2);
        Assert.Contains("Stock insuficiente", mensaje);
    }

    [Fact]
    public async Task EntregaParcialYLuegoTotal_BajaStockYActualizaEstado()
    {
        var (repo, _, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Completo);
        var productoId = await SeedProductoAsync(context, 10);
        var (_, pedido, _) = await repo.Crear(Pedido(productoId, 6));
        var lineaId = pedido!.Detalle[0].Id;

        var (e1, parcial, _) = await repo.RegistrarEntrega(pedido.Id, new CreateEntregaPayload { Detalle = new() { new() { PedidoVentaDetalleId = lineaId, Cantidad = 2 } } });
        Assert.Equal(ServiceStatus.Ok, e1);
        Assert.Equal(EstadosPedidoVenta.EntregadoParcial, parcial!.EstadoPedidoVenta);
        Assert.Equal(8, await StockAsync(context, productoId));

        var (e2, total, _) = await repo.RegistrarEntrega(pedido.Id, new CreateEntregaPayload { Detalle = new() { new() { PedidoVentaDetalleId = lineaId, Cantidad = 4 } } });
        Assert.Equal(ServiceStatus.Ok, e2);
        Assert.Equal(EstadosPedidoVenta.Entregado, total!.EstadoPedidoVenta);
        Assert.Equal(4, await StockAsync(context, productoId));
    }

    [Fact]
    public async Task Entrega_NoPermiteEntregarMasDeLoPendiente()
    {
        var (repo, _, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Completo);
        var productoId = await SeedProductoAsync(context, 10);
        var (_, pedido, _) = await repo.Crear(Pedido(productoId, 3));

        var (estado, _, _) = await repo.RegistrarEntrega(pedido!.Id, new CreateEntregaPayload { Detalle = new() { new() { PedidoVentaDetalleId = pedido.Detalle[0].Id, Cantidad = 4 } } });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Equal(10, await StockAsync(context, productoId));
    }

    [Fact]
    public async Task AnularEntrega_DevuelveElStockYReabreElPedido()
    {
        var (repo, _, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Completo);
        var productoId = await SeedProductoAsync(context, 10);
        var (_, pedido, _) = await repo.Crear(Pedido(productoId, 6));
        var (_, entregado, _) = await repo.RegistrarEntrega(pedido!.Id, new CreateEntregaPayload { Detalle = new() { new() { PedidoVentaDetalleId = pedido.Detalle[0].Id, Cantidad = 6 } } });

        var (estado, resultado, _) = await repo.AnularEntrega(entregado!.Entregas[0].Id);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(EstadosPedidoVenta.Confirmado, resultado!.EstadoPedidoVenta);
        Assert.Equal(10, await StockAsync(context, productoId));
    }

    private static ComprobantePayload Factura(int productoId, int cantidad, int pedidoVentaId, int tipo = 2) => new()
    {
        TipoDocumentoVentaId = tipo,
        Total = cantidad * 10m,
        PedidoVentaId = pedidoVentaId,
        DetalleComprobante = new() { new() { ProductoId = productoId, Cantidad = cantidad, ValorUnitario = 10m } },
        DetallePago = new()
    };

    [Fact]
    public async Task FacturaDesdeEntrega_NoDescuentaStockDosVecesYCierraElPedido()
    {
        var (repo, comprobante, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Completo);
        context.TipoDocumentoVenta.AddRange(
            new TipoDocumentoVenta { Id = 2, Nombre = "BOLETA" });
        await context.SaveChangesAsync();
        var productoId = await SeedProductoAsync(context, 10);
        var (_, pedido, _) = await repo.Crear(Pedido(productoId, 5));
        await repo.RegistrarEntrega(pedido!.Id, new CreateEntregaPayload { Detalle = new() { new() { PedidoVentaDetalleId = pedido.Detalle[0].Id, Cantidad = 5 } } });

        var (estado, _, mensaje) = await comprobante.CrearComprobante(Factura(productoId, 5, pedido.Id));

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(5, await StockAsync(context, productoId)); // solo bajo en la entrega
        var (_, final, _) = await repo.Obtener(pedido.Id);
        Assert.Equal(EstadosPedidoVenta.Cerrado, final!.EstadoPedidoVenta);
    }

    [Fact]
    public async Task Factura_NoPuedeFacturarMasDeLoEntregado()
    {
        var (repo, comprobante, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Completo);
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 2, Nombre = "BOLETA" });
        await context.SaveChangesAsync();
        var productoId = await SeedProductoAsync(context, 10);
        var (_, pedido, _) = await repo.Crear(Pedido(productoId, 5));
        await repo.RegistrarEntrega(pedido!.Id, new CreateEntregaPayload { Detalle = new() { new() { PedidoVentaDetalleId = pedido.Detalle[0].Id, Cantidad = 2 } } });

        var (estado, _, _) = await comprobante.CrearComprobante(Factura(productoId, 3, pedido.Id));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
    }

    [Fact]
    public async Task AnularFactura_DeEntrega_NoRestauraStockYReabreElPedido()
    {
        var (repo, comprobante, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Completo);
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 2, Nombre = "BOLETA" });
        await context.SaveChangesAsync();
        var productoId = await SeedProductoAsync(context, 10);
        var (_, pedido, _) = await repo.Crear(Pedido(productoId, 5));
        await repo.RegistrarEntrega(pedido!.Id, new CreateEntregaPayload { Detalle = new() { new() { PedidoVentaDetalleId = pedido.Detalle[0].Id, Cantidad = 5 } } });
        await comprobante.CrearComprobante(Factura(productoId, 5, pedido.Id));
        var comprobanteId = (await context.ComprobanteCabecera.AsNoTracking().SingleAsync()).Id;

        var (estado, _, mensaje) = await comprobante.AnularVenta(comprobanteId, "prueba");

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(5, await StockAsync(context, productoId)); // no se devolvio: la mercaderia sigue entregada
        var (_, final, _) = await repo.Obtener(pedido.Id);
        Assert.Equal(EstadosPedidoVenta.Entregado, final!.EstadoPedidoVenta);
    }

    [Fact]
    public async Task ModoSimplificado_ComprobanteConPedidoVentaId_EsRechazado()
    {
        var (_, comprobante, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentas(config, FlujoComprasModo.Simplificado);
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 2, Nombre = "BOLETA" });
        await context.SaveChangesAsync();
        var productoId = await SeedProductoAsync(context, 10);

        var (estado, _, mensaje) = await comprobante.CrearComprobante(Factura(productoId, 1, pedidoVentaId: 999));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("no está activo", mensaje);
        Assert.Equal(10, await StockAsync(context, productoId));
    }
}
