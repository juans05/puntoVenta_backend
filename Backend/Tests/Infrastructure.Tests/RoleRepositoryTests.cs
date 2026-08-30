using Domain.Entities.Identity;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class RoleRepositoryTests
{
    private static async Task SeedCatalogoAsync(Infrastructure.Data.SpaContext context)
    {
        context.AspNetModule.Add(new AspNetModule { Identificador = "200", Nombre = "Productos" });
        context.AspNetSubModule.Add(new AspNetSubModule { Identificador = "201", Nombre = "Productos", ModuloId = "200" });
        context.AspNetModule.Add(new AspNetModule { Identificador = "300", Nombre = "Ventas" });
        context.AspNetSubModule.Add(new AspNetSubModule { Identificador = "301", Nombre = "Ventas", ModuloId = "300" });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// AspNetUserRoles.UserId es FK obligatoria hacia AspNetUsers (User.UserId), y User a su
    /// vez cuelga de Tenant/Rubro (FKs no nulas) - hace falta la cadena completa para poder
    /// insertar una fila real y ejercitar la validacion de "rol con usuarios asignados".
    /// </summary>
    private static async Task<string> SeedUsuarioAsync(Infrastructure.Data.SpaContext context, string userId)
    {
        var rubro = new Domain.Entities.Rubro { Nombre = "Test" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var tenant = new Domain.Entities.Tenant { Identificador = 1, Name = "TEST", TenantKey = "TEST", RubroId = rubro.Id };
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

        return userId;
    }

    private static RoleManager<Role> BuildRoleManager(Infrastructure.Data.SpaContext context)
    {
        var store = new RoleStore<Role, Infrastructure.Data.SpaContext, string>(context);

        // KeyNormalizer y ErrorDescriber deben ser instancias reales: con KeyNormalizer null,
        // RoleManager.CreateAsync deja NormalizedName sin uppercasear, y el chequeo de
        // duplicados de CrearRol (que compara contra Nombre.ToUpper()) deja de detectar
        // duplicados con distinta capitalizacion.
        return new RoleManager<Role>(
            store,
            new IRoleValidator<Role>[] { new RoleValidator<Role>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!);
    }

    [Fact]
    public async Task CrearRol_AsignaSubmodulosYQuedaVisibleEnListarRoles()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));

        var (estado, creado, _) = await repo.CrearRol(new CreateRolePayload
        {
            Nombre = "Ventas",
            RutaPorDefecto = "/facturacion",
            Prioridad = 20,
            SubmoduleIds = new List<string> { "301" }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(new List<string> { "301" }, creado!.SubmoduleIds);

        var (estadoLista, lista, _) = await repo.ListarRoles();
        Assert.Equal(ServiceStatus.Ok, estadoLista);
        Assert.Single(lista!);
        Assert.Equal("Ventas", lista![0].Nombre);
    }

    [Fact]
    public async Task CrearRol_NombreDuplicadoEnElMismoTenant_Falla()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));

        await repo.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });
        var (estado, _, mensaje) = await repo.CrearRol(new CreateRolePayload { Nombre = "ventas", Prioridad = 30 });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("Ya existe", mensaje);
    }

    [Fact]
    public async Task DosTenants_PuedenTenerCadaUnoUnRolConElMismoNombre()
    {
        var (contextA, connectionA) = TestDbContextFactory.CreateContext(new FakeTenantResolver(tenantName: "TENANT_A"));
        using var __ = connectionA;
        await SeedCatalogoAsync(contextA);
        var repoA = new RoleRepository(contextA, BuildRoleManager(contextA));
        var (estadoA, _, _) = await repoA.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });

        var (contextB, connectionB) = TestDbContextFactory.CreateContext(new FakeTenantResolver(tenantName: "TENANT_B"));
        using var ___ = connectionB;
        await SeedCatalogoAsync(contextB);
        var repoB = new RoleRepository(contextB, BuildRoleManager(contextB));
        var (estadoB, _, _) = await repoB.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });

        Assert.Equal(ServiceStatus.Ok, estadoA);
        Assert.Equal(ServiceStatus.Ok, estadoB);
    }

    [Fact]
    public async Task EliminarRol_ConUsuariosAsignados_Falla()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));
        var (_, creado, _) = await repo.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });

        var userId = await SeedUsuarioAsync(context, "user-1");
        context.UserRoles.Add(new UserRol { UserId = userId, RoleId = creado!.Id });
        await context.SaveChangesRegularAsync();

        var (estado, mensaje) = await repo.EliminarRol(creado.Id);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("1 usuario", mensaje);
    }

    [Fact]
    public async Task ResolverAccesoUsuario_ConDosRoles_UneSubmodulosYGanaLaMenorPrioridad()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));

        await repo.CrearRol(new CreateRolePayload { Nombre = "Ventas", RutaPorDefecto = "/facturacion", Prioridad = 20, SubmoduleIds = new List<string> { "301" } });
        await repo.CrearRol(new CreateRolePayload { Nombre = "Admin", RutaPorDefecto = "/dashboard/productos", Prioridad = 0, SubmoduleIds = new List<string> { "201", "301" } });

        var (rutas, rutaPorDefecto) = await repo.ResolverAccesoUsuario(new List<string> { "Ventas", "Admin" });

        Assert.Equal("/dashboard/productos", rutaPorDefecto); // gana Admin (prioridad 0)
        Assert.Equal(2, rutas.Sum(r => r.SubModulos.Count)); // 201 + 301, sin duplicar
    }

    [Fact]
    public async Task ResolverAccesoUsuario_SinRoles_DevuelveVacioSinRutaPorDefecto()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var repo = new RoleRepository(context, BuildRoleManager(context));

        var (rutas, rutaPorDefecto) = await repo.ResolverAccesoUsuario(new List<string>());

        Assert.Empty(rutas);
        Assert.Null(rutaPorDefecto);
    }
}
