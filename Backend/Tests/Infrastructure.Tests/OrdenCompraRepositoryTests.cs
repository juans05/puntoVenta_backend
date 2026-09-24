using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class OrdenCompraRepositoryTests
{
    private static (OrdenCompraRepository Orden, CompraRepository Compra, SpaContext Context, System.Data.Common.DbConnection Connection) Preparar()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        var compraRepo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);
        var repo = new OrdenCompraRepository(context, compraRepo, httpContextAccessor: null);
        return (repo, compraRepo, context, connection);
    }

    private static Task ConfigurarAsync(OrdenCompraRepository repo, string flujo = FlujoComprasModo.Completo,
        string cruce = CruceFacturaModo.Advertir, decimal? umbral = null)
        => repo.GuardarConfiguracion(new ConfiguracionFlujoPayload { FlujoCompras = flujo, CruceFactura = cruce, MontoAprobacionOc = umbral });

    private static async Task<int> SeedProductoAsync(SpaContext context, int stock = 0)
    {
        var p = new Producto { Nombre = "Producto OC", Precio = 10, Stock = stock, RestriccionEdad = 0 };
        context.Producto.Add(p);
        await context.SaveChangesAsync();
        return p.Id;
    }

    private static CreateOrdenCompraPayload Orden(int productoId, int cantidad = 10, decimal costo = 5m) => new()
    {
        Detalle = new() { new() { ProductoId = productoId, Cantidad = cantidad, CostoUnitario = costo } }
    };

    private static FacturarOrdenCompraPayload Factura(int productoId, int cantidad, decimal costo, bool confirmar = false) => new()
    {
        Serie = "F001",
        Numero = "1",
        ConfirmarDiferencias = confirmar,
        Detalle = new() { new() { ProductoId = productoId, Cantidad = cantidad, CostoUnitario = costo } }
    };

    [Fact]
    public async Task ModoSimplificado_NoPermiteCrearOrdenes()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var productoId = await SeedProductoAsync(context);

        var (estado, _, mensaje) = await repo.CrearOrden(Orden(productoId));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("no está activo", mensaje);
    }

    [Fact]
    public async Task ModoSimplificado_CrearCompraSigueSubiendoElStock()
    {
        var (repo, compraRepo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo, FlujoComprasModo.Simplificado);
        var productoId = await SeedProductoAsync(context);

        var (estado, _, _) = await compraRepo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new() { new() { ProductoId = productoId, Cantidad = 4, CostoUnitario = 5m } }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(4, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock);
    }

    [Fact]
    public async Task RecepcionParcialYLuegoTotal_SubeStockYActualizaEstado()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);
        var productoId = await SeedProductoAsync(context);

        var (_, orden, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10));
        Assert.Equal(EstadoOrdenCompra.Emitida, orden!.EstadoOrden);
        var lineaId = orden.Detalle[0].Id;

        var (e1, parcial, _) = await repo.RegistrarRecepcion(orden.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = lineaId, Cantidad = 4 } } });
        Assert.Equal(ServiceStatus.Ok, e1);
        Assert.Equal(EstadoOrdenCompra.RecibidaParcial, parcial!.EstadoOrden);
        Assert.Equal(4, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock);

        var (e2, total, _) = await repo.RegistrarRecepcion(orden.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = lineaId, Cantidad = 6 } } });
        Assert.Equal(ServiceStatus.Ok, e2);
        Assert.Equal(EstadoOrdenCompra.Recibida, total!.EstadoOrden);
        Assert.Equal(10, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock);
    }

    [Fact]
    public async Task Recepcion_NoPermiteRecibirMasDeLoPendiente()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);
        var productoId = await SeedProductoAsync(context);
        var (_, orden, _) = await repo.CrearOrden(Orden(productoId, cantidad: 5));

        var (estado, _, _) = await repo.RegistrarRecepcion(orden!.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = orden.Detalle[0].Id, Cantidad = 6 } } });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Equal(0, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock);
    }

    [Fact]
    public async Task Aprobacion_PorMonto_BajoElUmbralEmiteYSobreElUmbralQuedaPendiente()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo, umbral: 100m);
        var productoId = await SeedProductoAsync(context);

        var (_, chica, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10, costo: 5m)); // 50
        var (_, grande, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10, costo: 20m)); // 200

        Assert.Equal(EstadoOrdenCompra.Emitida, chica!.EstadoOrden);
        Assert.Equal(EstadoOrdenCompra.PendienteAprobacion, grande!.EstadoOrden);

        // Pendiente no se puede recibir hasta que se apruebe.
        var (estado, _, _) = await repo.RegistrarRecepcion(grande.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = grande.Detalle[0].Id, Cantidad = 1 } } });
        Assert.Equal(ServiceStatus.FailedValidation, estado);

        var (_, aprobada, _) = await repo.AprobarOrden(grande.Id, "admin");
        Assert.Equal(EstadoOrdenCompra.Emitida, aprobada!.EstadoOrden);
        Assert.Equal("admin", aprobada.AprobadoPor);
    }

    [Fact]
    public async Task AnularRecepcion_RevierteElStockYReabreLaOrden()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);
        var productoId = await SeedProductoAsync(context);
        var (_, orden, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10));
        var (_, recibida, _) = await repo.RegistrarRecepcion(orden!.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = orden.Detalle[0].Id, Cantidad = 10 } } });

        var (estado, resultado, _) = await repo.AnularRecepcion(recibida!.Recepciones[0].Id);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(EstadoOrdenCompra.Emitida, resultado!.EstadoOrden);
        Assert.Equal(0, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock);
    }

    [Fact]
    public async Task Factura_QueCuadra_NoTocaElStockYCierraLaOrden()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);
        var productoId = await SeedProductoAsync(context);
        var (_, orden, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10, costo: 5m));
        await repo.RegistrarRecepcion(orden!.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = orden.Detalle[0].Id, Cantidad = 10 } } });

        var (estado, compra, _) = await repo.FacturarOrden(orden.Id, Factura(productoId, 10, 5m));

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(compra);
        Assert.Equal(10, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock); // no se duplico
        var (_, ordenFinal, _) = await repo.ObtenerOrden(orden.Id);
        Assert.Equal(EstadoOrdenCompra.Cerrada, ordenFinal!.EstadoOrden);
    }

    [Fact]
    public async Task Factura_ConDiferenciaDePrecio_AdvertirPideConfirmarYBloquearRechaza()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo, cruce: CruceFacturaModo.Advertir);
        var productoId = await SeedProductoAsync(context);
        var (_, orden, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10, costo: 5m));
        await repo.RegistrarRecepcion(orden!.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = orden.Detalle[0].Id, Cantidad = 10 } } });

        var (sinConfirmar, _, msg) = await repo.FacturarOrden(orden.Id, Factura(productoId, 10, 6m));
        Assert.Equal(ServiceStatus.FailedValidation, sinConfirmar);
        Assert.StartsWith("[DIFERENCIAS]", msg);

        var (confirmado, _, _) = await repo.FacturarOrden(orden.Id, Factura(productoId, 10, 6m, confirmar: true));
        Assert.Equal(ServiceStatus.Ok, confirmado);

        // En modo BLOQUEAR ni confirmando pasa.
        var (repo2, _, context2, connection2) = Preparar();
        using var ___ = connection2; using var ____ = context2;
        await ConfigurarAsync(repo2, cruce: CruceFacturaModo.Bloquear);
        var producto2 = await SeedProductoAsync(context2);
        var (_, orden2, _) = await repo2.CrearOrden(Orden(producto2, cantidad: 10, costo: 5m));
        await repo2.RegistrarRecepcion(orden2!.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = orden2.Detalle[0].Id, Cantidad = 10 } } });

        var (bloqueado, _, _) = await repo2.FacturarOrden(orden2.Id, Factura(producto2, 10, 6m, confirmar: true));
        Assert.Equal(ServiceStatus.FailedValidation, bloqueado);
    }

    [Fact]
    public async Task AnularFactura_DeOrden_NoRevierteStockYReabreLaOrden()
    {
        var (repo, compraRepo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);
        var productoId = await SeedProductoAsync(context);
        var (_, orden, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10, costo: 5m));
        await repo.RegistrarRecepcion(orden!.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = orden.Detalle[0].Id, Cantidad = 10 } } });
        var (_, compra, _) = await repo.FacturarOrden(orden.Id, Factura(productoId, 10, 5m));

        var (estado, _, _) = await compraRepo.AnularCompra(compra!.Id);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(10, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock);
        var (_, ordenFinal, _) = await repo.ObtenerOrden(orden.Id);
        Assert.Equal(EstadoOrdenCompra.Recibida, ordenFinal!.EstadoOrden);
    }
}
