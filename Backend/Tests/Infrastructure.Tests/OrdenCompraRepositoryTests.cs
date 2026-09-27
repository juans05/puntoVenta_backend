using Domain.Entities;
using Domain.Entities.Identity;
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
        var repo = new OrdenCompraRepository(context, compraRepo, new DepartamentoRepository(context), httpContextAccessor: null);
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

    private static CreateOrdenCompraPayload OrdenServicio(string descripcion = "Mantenimiento", int cantidad = 1, decimal costo = 500m) => new()
    {
        TipoOrden = TipoOrdenCompra.Servicio,
        Detalle = new() { new() { Descripcion = descripcion, Cantidad = cantidad, CostoUnitario = costo } }
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

    // DepartamentoAprobador.UserId es FK real hacia AspNetUsers: hace falta la fila de usuario
    // (con su cadena Rubro/Tenant, igual que RoleRepositoryTests.SeedUsuarioAsync) para poder
    // asignarlo como aprobador.
    private static async Task SeedUsuarioAsync(SpaContext context, string userId)
    {
        var rubro = new Rubro { Nombre = "Test" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var tenant = new Tenant { Identificador = 1, Name = "TEST", TenantKey = "TEST", RubroId = rubro.Id };
        context.Tenant.Add(tenant);
        await context.SaveChangesAsync();

        context.Users.Add(new User
        {
            Id = userId,
            UserName = userId,
            FirstName = "Test",
            LastName = "User",
            FechaCreacion = DateTime.UtcNow.ToString(),
            Estado = true,
            TenantId = tenant.Identificador
        });
        await context.SaveChangesRegularAsync();
    }

    private static async Task<int> SeedDepartamentoAsync(SpaContext context, string aprobadorId)
    {
        await SeedUsuarioAsync(context, aprobadorId);
        var departamento = new Departamento { Nombre = "Logística" };
        context.Departamento.Add(departamento);
        await context.SaveChangesAsync();
        context.DepartamentoAprobador.Add(new DepartamentoAprobador { DepartamentoId = departamento.Id, UserId = aprobadorId });
        await context.SaveChangesAsync();
        return departamento.Id;
    }

    [Fact]
    public async Task Aprobacion_PorMonto_BajoElUmbralEmiteDirectoYSobreElUmbralQuedaPendiente()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo, umbral: 100m);
        var productoId = await SeedProductoAsync(context);
        var departamentoId = await SeedDepartamentoAsync(context, "aprobador-1");

        var (_, chica, _) = await repo.CrearOrden(Orden(productoId, cantidad: 10, costo: 5m)); // 50: no pide nada
        var (estadoSinDepto, _, mensaje) = await repo.CrearOrden(Orden(productoId, cantidad: 10, costo: 20m)); // 200, sin elegir departamento
        var (_, grande, _) = await repo.CrearOrden(new CreateOrdenCompraPayload
        {
            DepartamentoId = departamentoId,
            AprobadorAsignadoId = "aprobador-1",
            Detalle = new() { new() { ProductoId = productoId, Cantidad = 10, CostoUnitario = 20m } }
        });

        Assert.Equal(EstadoOrdenCompra.Emitida, chica!.EstadoOrden);
        Assert.Equal(ServiceStatus.FailedValidation, estadoSinDepto);
        Assert.Contains("departamento", mensaje);
        Assert.Equal(EstadoOrdenCompra.PendienteAprobacion, grande!.EstadoOrden);
        Assert.Equal("aprobador-1", grande.AprobadorAsignadoId);

        // Pendiente no se puede recibir hasta que se apruebe.
        var (estado, _, _) = await repo.RegistrarRecepcion(grande.Id, new CreateRecepcionPayload { Detalle = new() { new() { OrdenCompraDetalleId = grande.Detalle[0].Id, Cantidad = 1 } } });
        Assert.Equal(ServiceStatus.FailedValidation, estado);
    }

    [Fact]
    public async Task AprobarOrden_SoloElAprobadorAsignadoOUnAdministrador()
    {
        var (repo, compraRepo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo, umbral: 100m);
        var productoId = await SeedProductoAsync(context);
        var departamentoId = await SeedDepartamentoAsync(context, "aprobador-1");
        var (_, orden, _) = await repo.CrearOrden(new CreateOrdenCompraPayload
        {
            DepartamentoId = departamentoId,
            AprobadorAsignadoId = "aprobador-1",
            Detalle = new() { new() { ProductoId = productoId, Cantidad = 10, CostoUnitario = 20m } }
        });

        var repoOtroUsuario = new OrdenCompraRepository(context, compraRepo, new DepartamentoRepository(context),
            new FakeHttpContextAccessor("otro", userId: "otro-usuario"));
        var (estadoRechazado, _, mensajeRechazo) = await repoOtroUsuario.AprobarOrden(orden!.Id, "otro");
        Assert.Equal(ServiceStatus.FailedValidation, estadoRechazado);
        Assert.Contains("aprobador asignado", mensajeRechazo);

        var repoAdmin = new OrdenCompraRepository(context, compraRepo, new DepartamentoRepository(context),
            new FakeHttpContextAccessor("admin", userId: "admin-id", "Administrador"));
        var (estadoAdmin, aprobadaPorAdmin, _) = await repoAdmin.AprobarOrden(orden.Id, "admin");
        Assert.Equal(ServiceStatus.Ok, estadoAdmin);
        Assert.Equal(EstadoOrdenCompra.Emitida, aprobadaPorAdmin!.EstadoOrden);
        Assert.Equal("admin", aprobadaPorAdmin.AprobadoPor);
    }

    [Fact]
    public async Task AprobarOrden_ElAprobadorAsignadoSiPuede()
    {
        var (repo, compraRepo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo, umbral: 100m);
        var productoId = await SeedProductoAsync(context);
        var departamentoId = await SeedDepartamentoAsync(context, "aprobador-1");
        var (_, orden, _) = await repo.CrearOrden(new CreateOrdenCompraPayload
        {
            DepartamentoId = departamentoId,
            AprobadorAsignadoId = "aprobador-1",
            Detalle = new() { new() { ProductoId = productoId, Cantidad = 10, CostoUnitario = 20m } }
        });

        var repoAprobador = new OrdenCompraRepository(context, compraRepo, new DepartamentoRepository(context),
            new FakeHttpContextAccessor("jefe", userId: "aprobador-1"));
        var (estado, aprobada, _) = await repoAprobador.AprobarOrden(orden!.Id, "jefe");

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(EstadoOrdenCompra.Emitida, aprobada!.EstadoOrden);
    }

    [Fact]
    public async Task CrearOrden_AprobadorQueNoPerteneceAlDepartamento_EsRechazado()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo, umbral: 100m);
        var productoId = await SeedProductoAsync(context);
        var departamentoId = await SeedDepartamentoAsync(context, "aprobador-1");

        var (estado, _, mensaje) = await repo.CrearOrden(new CreateOrdenCompraPayload
        {
            DepartamentoId = departamentoId,
            AprobadorAsignadoId = "no-es-aprobador",
            Detalle = new() { new() { ProductoId = productoId, Cantidad = 10, CostoUnitario = 20m } }
        });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("no es aprobador", mensaje);
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

    [Fact]
    public async Task OrdenServicio_AlEmitirse_QuedaRecibidaSinInventoryMovement()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);

        var (estado, orden, _) = await repo.CrearOrden(OrdenServicio());

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(EstadoOrdenCompra.Recibida, orden!.EstadoOrden);
        Assert.Equal(1, orden.Detalle[0].CantidadRecibida);
        Assert.Empty(await context.InventoryMovement.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task OrdenServicio_NoSePuedeRecibir()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);
        var (_, orden, _) = await repo.CrearOrden(OrdenServicio());

        var (estado, _, mensaje) = await repo.RegistrarRecepcion(orden!.Id, new CreateRecepcionPayload
        {
            Detalle = new() { new() { OrdenCompraDetalleId = orden.Detalle[0].Id, Cantidad = 1 } }
        });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("no requiere recepción", mensaje);
    }

    [Fact]
    public async Task OrdenServicio_SePuedeFacturarDirectoSinRecepcionYNoTocaStock()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);
        var productoId = await SeedProductoAsync(context, stock: 3); // producto ajeno, para confirmar que no se toca
        var (_, orden, _) = await repo.CrearOrden(OrdenServicio(cantidad: 1, costo: 500m));

        var (estado, compra, mensaje) = await repo.FacturarOrden(orden!.Id, new FacturarOrdenCompraPayload
        {
            Serie = "F001",
            Numero = "1",
            Detalle = new() { new() { OrdenCompraDetalleId = orden.Detalle[0].Id, Cantidad = 1, CostoUnitario = 500m } }
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(3, (await context.Producto.AsNoTracking().FirstAsync(p => p.Id == productoId)).Stock);
        Assert.Null(compra!.Detalle[0].ProductoId);
        Assert.Equal("Mantenimiento", compra.Detalle[0].Descripcion);
        var (_, ordenFinal, _) = await repo.ObtenerOrden(orden.Id);
        Assert.Equal(EstadoOrdenCompra.Cerrada, ordenFinal!.EstadoOrden);
    }

    [Fact]
    public async Task OrdenServicio_SinDescripcion_EsRechazada()
    {
        var (repo, _, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarAsync(repo);

        var (estado, _, mensaje) = await repo.CrearOrden(new CreateOrdenCompraPayload
        {
            TipoOrden = TipoOrdenCompra.Servicio,
            Detalle = new() { new() { Cantidad = 1, CostoUnitario = 100m } }
        });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("descripción", mensaje);
    }
}
