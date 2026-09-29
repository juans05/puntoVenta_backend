using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class ProveedorRepositoryTests
{
    [Fact]
    public async Task CrearProveedor_SinEmailNiTelefonoNiCelular_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new ProveedorRepository(context, TestDbContextFactory.Mapper);

        var (estado, proveedor, mensaje) = await repo.CrearProveedor(new CreateProveedorPayload { Nombre = "Distribuidora SAC" });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Null(proveedor);
        Assert.Equal("El email es obligatorio", mensaje);
    }

    [Fact]
    public async Task CrearProveedor_ConEmailPeroSinTelefonoNiCelular_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new ProveedorRepository(context, TestDbContextFactory.Mapper);

        var (estado, _, mensaje) = await repo.CrearProveedor(new CreateProveedorPayload { Nombre = "Distribuidora SAC", Email = "ventas@distribuidora.com" });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Equal("El teléfono/celular es obligatorio", mensaje);
    }

    [Fact]
    public async Task CrearProveedor_ConEmailYCelular_Ok()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new ProveedorRepository(context, TestDbContextFactory.Mapper);

        var (estado, proveedor, _) = await repo.CrearProveedor(new CreateProveedorPayload
        { Nombre = "Distribuidora SAC", Email = "ventas@distribuidora.com", Celular = "999999999" });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(proveedor);
    }
}
