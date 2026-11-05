using Domain.Entities;
using Domain.Models;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class TenantAppRepositoryTests
{
    private static TenantAppRepository Crear(out Infrastructure.Data.SpaContext context)
    {
        (context, _) = TestDbContextFactory.CreateContext();
        var rubroId = context.Rubro.First().Id;
        context.Tenant.Add(new Tenant { Identificador = 1, Name = "TEST", TenantKey = "TEST", RubroId = rubroId });
        context.SaveChanges();
        return new TenantAppRepository(context);
    }

    [Fact]
    public async Task SinFilas_DevuelveSoloArp()
    {
        var repo = Crear(out _);
        Assert.Equal(new[] { "arp" }, await repo.AppsContratadas(1));
    }

    [Fact]
    public async Task ActivarArm_EnEmpresaSinFilas_ConservaArp()
    {
        var repo = Crear(out _);
        await repo.SetApp(1, "arm", true);
        Assert.Equal(new[] { "arm", "arp" }, await repo.AppsContratadas(1));
    }

    [Fact]
    public async Task DesactivarArp_DejaSoloArm()
    {
        var repo = Crear(out _);
        await repo.SetApp(1, "arm", true);
        await repo.SetApp(1, "arp", false);
        Assert.Equal(new[] { "arm" }, await repo.AppsContratadas(1));
    }

    [Fact]
    public async Task AppDesconocida_EsFailedValidation()
    {
        var repo = Crear(out _);
        Assert.Equal(ServiceStatus.FailedValidation, (await repo.SetApp(1, "xyz", true)).Item1);
    }

    [Fact]
    public async Task TenantInexistente_EsNotFound()
    {
        var repo = Crear(out _);
        Assert.Equal(ServiceStatus.NotFound, (await repo.SetApp(99, "arm", true)).Item1);
    }
}
