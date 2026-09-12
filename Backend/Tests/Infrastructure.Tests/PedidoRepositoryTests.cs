using Domain.Entities;
using Domain.Payloads;
using Domain.Tenant;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class PedidoRepositoryTests
{
    /// <summary>
    /// Regression test: Pedido.SucursalId used to be a non-nullable int, so the global query
    /// filter's "SucursalId == null" branch was always false (dead code), and CrearPedido used
    /// to fall back to a client-supplied SucursalId when the tenant context had none. Together
    /// that made a Pedido created by a session with no resolved sucursal (no X-Sucursal header,
    /// no Sucursal claim) invisible in ListarPedidos for that same session.
    /// </summary>
    [Fact]
    public async Task CrearPedido_SinSucursalEnElContexto_QuedaVisibleEnListarPedidos()
    {
        var tenantResolver = new FakeTenantResolver(sucursalId: null);
        var (context, connection) = TestDbContextFactory.CreateContext(tenantResolver);
        using (connection)
        using (context)
        {
            TenantContextAccessor.Set(new TenantContext { Name = "TEST", SucursalId = null, Username = "TEST_USER" });
            var tenantContextAccessor = new TenantContextAccessor();

            var producto = new Producto { Nombre = "Producto test", Precio = 10 };
            context.Producto.Add(producto);
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context, TestDbContextFactory.Mapper, tenantContextAccessor);

            var (estadoCrear, pedidoCreado, _) = await repo.CrearPedido(new CreatePedidoPayload
            {
                SucursalId = 999, // valor de cliente que el fix ignora a propósito
                Total = 10,
                Detalles = new List<CreatePedidoDetallePayload>
                {
                    new() { ProductoId = producto.Id, Cantidad = 1, ValorUnitario = 10 }
                }
            });

            Assert.Equal(Domain.Models.ServiceStatus.Ok, estadoCrear);
            Assert.NotNull(pedidoCreado);

            var (_, lista, _) = await repo.ListarPedidos(new PedidoQueryParams { Page = 1, Amount = 10 });

            Assert.Contains(lista!.Items!, p => p.Id == pedidoCreado!.Id);
        }
    }

    private static async Task<(SpaContext Context, System.Data.Common.DbConnection Connection, PedidoRepository Repo, string Token)> PrepararPedidoProvincia(string ubigeoId)
    {
        var tenantResolver = new FakeTenantResolver(sucursalId: null);
        var (context, connection) = TestDbContextFactory.CreateContext(tenantResolver);
        TenantContextAccessor.Set(new TenantContext { Name = "TEST", SucursalId = null, Username = "TEST_USER" });
        var tenantContextAccessor = new TenantContextAccessor();

        context.Ubigeo.Add(new Ubigeo { UbigeoId = ubigeoId, Departamento = "AREQUIPA", Provincia = "AREQUIPA", Distrito = "AREQUIPA" });
        context.TipoDocumento.Add(new TipoDocumento { Id = 1, Nombre = "DNI" });
        var producto = new Producto { Nombre = "Producto test", Precio = 10 };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();

        var repo = new PedidoRepository(context, TestDbContextFactory.Mapper, tenantContextAccessor);
        var (_, pedidoCreado, _) = await repo.CrearPedido(new CreatePedidoPayload
        {
            SucursalId = 0,
            Total = 10,
            Detalles = new List<CreatePedidoDetallePayload>
            {
                new() { ProductoId = producto.Id, Cantidad = 1, ValorUnitario = 10 }
            }
        });

        return (context, connection, repo, pedidoCreado!.Token);
    }

    [Fact]
    public async Task SubmitPedidoPublico_ProvinciaSinSalon_FallaValidacion()
    {
        var (context, connection, repo, token) = await PrepararPedidoProvincia("040101");
        using (connection)
        using (context)
        {
            var (estado, resultado, _) = await repo.SubmitPedidoPublico(token, new PedidoPublicoSubmitPayload
            {
                Nombre = "Juan Perez",
                Dni = "12345678",
                Celular = "999999999",
                UbigeoId = "040101",
                SalonId = null
            });

            Assert.Equal(Domain.Models.ServiceStatus.FailedValidation, estado);
            Assert.False(resultado.Exito);
        }
    }

    [Fact]
    public async Task SubmitPedidoPublico_ProvinciaConSalonValido_GuardaCurrierYSalon()
    {
        var (context, connection, repo, token) = await PrepararPedidoProvincia("040101");
        using (connection)
        using (context)
        {
            var salon = new Salon { Nombre = "Shalom Arequipa", UbigeoId = "040101", Activo = true };
            context.Salon.Add(salon);
            await context.SaveChangesAsync();

            var (estado, resultado, _) = await repo.SubmitPedidoPublico(token, new PedidoPublicoSubmitPayload
            {
                Nombre = "Juan Perez",
                Dni = "12345678",
                Celular = "999999999",
                UbigeoId = "040101",
                SalonId = salon.Id
            });

            Assert.Equal(Domain.Models.ServiceStatus.Ok, estado);
            Assert.True(resultado.Exito);

            var pedido = await context.Pedido.IgnoreQueryFilters().FirstAsync(p => p.Token == token);
            Assert.Equal("SHALOM", pedido.Currier);
            Assert.Equal(salon.Id, pedido.SalonId);
        }
    }
}
