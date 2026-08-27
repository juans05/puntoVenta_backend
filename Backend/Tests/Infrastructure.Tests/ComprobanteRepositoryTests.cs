using Domain.Common;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class ComprobanteRepositoryTests
{
    private static async Task<int> SeedProductoAsync(Infrastructure.Data.SpaContext context, int stock)
    {
        var producto = new Producto { Nombre = "Producto test", Precio = 10m, Stock = stock, RestriccionEdad = 0 };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();
        return producto.Id;
    }

    private static async Task SeedTipoDocumentoVentaAsync(Infrastructure.Data.SpaContext context)
    {
        // Catalogo global (sin query filter): sembrado directo por Id, como hace el seed real.
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 1))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 1, Nombre = "Factura" });
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 2))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 2, Nombre = "Boleta" });
        await context.SaveChangesAsync();
    }

    private static async Task<int> SeedMetodoPagoAsync(Infrastructure.Data.SpaContext context)
    {
        var metodo = new Metodopago { Nombre = "Efectivo", Descripcion = "Efectivo" };
        context.Metodopago.Add(metodo);
        await context.SaveChangesAsync();
        return metodo.Id;
    }

    [Fact]
    public async Task CrearComprobante_SinSeriecorrelativoPrevio_LoCreaYArrancaEnUno()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2, // Boleta
            Total = 20m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        // Precondicion: no existe ninguna fila de Seriecorrelativo todavia.
        Assert.False(await context.Seriecorrelativo.AnyAsync());

        var (estado, resp, message) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var correlativo = await context.Seriecorrelativo.SingleAsync(x => x.Serie == "B001" && x.TipoDocumentoVentaId == 2);
        Assert.Equal(1, correlativo.Correlativo);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(1, cabecera.Correlativo);
        Assert.Equal("B001", cabecera.Serie);

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(8, producto.Stock); // 10 - 2
    }

    [Fact]
    public async Task CrearComprobante_DosVentasSeguidas_IncrementaElMismoCorrelativo()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        ComprobantePayload BuildPayload() => new()
        {
            TipoDocumentoVentaId = 2,
            Total = 10m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 1, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 10m }
            }
        };

        var (estado1, _, _) = await repo.CrearComprobante(BuildPayload());
        var (estado2, _, _) = await repo.CrearComprobante(BuildPayload());

        Assert.Equal(ServiceStatus.Ok, estado1);
        Assert.Equal(ServiceStatus.Ok, estado2);

        var correlativo = await context.Seriecorrelativo.SingleAsync(x => x.Serie == "B001" && x.TipoDocumentoVentaId == 2);
        Assert.Equal(2, correlativo.Correlativo);
    }

    [Fact]
    public async Task AnularVenta_RevierteStockYRegistraMovimientoDevolucionVenta()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 5); // ya vendido: stock post-venta

        var cabecera = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 20m,
            ValorSubtotal = 20m,
            ValorIgv = 0m,
            EstadoComprobante = EstatusComprobante.Creado,
        };
        context.ComprobanteCabecera.Add(cabecera);
        await context.SaveChangesAsync();

        context.ComprobanteDetalle.Add(new ComprobanteDetalle
        {
            ComprobanteCabeceraId = cabecera.Id,
            ProductoId = productoId,
            Cantidad = 2,
            ValorUnitario = 10m,
            ValorUnitarioTotal = 20m,
            ValorIgv = 0m,
        });
        await context.SaveChangesAsync();

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var (estado, _, _) = await repo.AnularVenta(cabecera.Id, "Cliente se arrepintio");

        Assert.Equal(ServiceStatus.Ok, estado);

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(7, producto.Stock); // 5 + 2 repuestos

        var cabeceraActualizada = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(EstatusComprobante.Anulado, cabeceraActualizada.EstadoComprobante);

        var movimiento = await context.InventoryMovement.SingleAsync();
        Assert.Equal((int)TipoMovimientoInventario.DevolucionVenta, movimiento.TipoMovimiento);
        Assert.Equal(2, movimiento.Cantidad);
        Assert.Equal(5, movimiento.StockAnterior);
        Assert.Equal(7, movimiento.StockPosterior);
        Assert.Equal("VentaAnulada", movimiento.ReferenciaTipo);
        Assert.Equal(cabecera.Id, movimiento.ReferenciaId);
    }

    [Fact]
    public async Task AnularVenta_SiYaEstaAnulada_NoDuplicaLaDevolucionDeStock()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 5);

        var cabecera = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 10m,
            ValorSubtotal = 10m,
            ValorIgv = 0m,
            EstadoComprobante = EstatusComprobante.Anulado,
        };
        context.ComprobanteCabecera.Add(cabecera);
        await context.SaveChangesAsync();

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var (estado, _, message) = await repo.AnularVenta(cabecera.Id, "Motivo");

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.False(await context.InventoryMovement.AnyAsync());

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(5, producto.Stock); // sin cambios
    }

    [Fact]
    public async Task ListarComprobantes_ExcluyeVentasAnuladas()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);

        context.ComprobanteCabecera.Add(new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 10m,
            EstadoComprobante = EstatusComprobante.Creado,
        });
        context.ComprobanteCabecera.Add(new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 2,
            ValorTotal = 99m,
            EstadoComprobante = EstatusComprobante.Anulado,
        });
        await context.SaveChangesAsync();

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var (estado, resultado, _) = await repo.ListarComprobantes(new ComprobanteQueryParams { Page = 1, Amount = 20 });

        Assert.Equal(ServiceStatus.Ok, estado);
        var lista = Assert.IsType<DataCollection<Domain.DTO.ComprobanteCabeceraDTO>>(resultado);
        Assert.Single(lista.Items);
        Assert.Equal(10m, lista.Items.Single().ValorTotal);
    }
}
