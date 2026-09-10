using Domain.Entities;
using Xunit;

namespace Infrastructure.Tests;

public class TenantRegistryTests
{
    [Fact]
    public async Task GetActiveTenants_DevuelveSoloTenantsActivos()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var rubro = new Domain.Entities.Rubro { Nombre = "Barbería" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        context.Tenant.Add(new Tenant { Identificador = 1, Name = "ACTIVO1", TenantKey = "activo", RubroId = rubro.Id, Activo = true });
        context.Tenant.Add(new Tenant { Identificador = 2, Name = "INACTIVO1", TenantKey = "inactivo", RubroId = rubro.Id, Activo = false });
        await context.SaveChangesAsync();

        var result = TenantRegistry.GetActiveTenants(context);

        Assert.Single(result);
        Assert.Equal("ACTIVO1", result[0].Name);
        Assert.Equal("activo", result[0].TenantKey);
    }

    [Fact]
    public async Task GetActiveTenants_SinTenants_DevuelveArregloVacio()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var result = TenantRegistry.GetActiveTenants(context);

        Assert.Empty(result);
    }
}
