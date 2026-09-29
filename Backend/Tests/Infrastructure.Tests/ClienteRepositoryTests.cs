using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class ClienteRepositoryTests
{
    [Fact]
    public async Task CreateCliente_SinEmailNiTelefono_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new ClienteRepository(context, TestDbContextFactory.Mapper);

        var (estado, cliente, mensaje) = await repo.CreateCliente(new CreateClientePayload { Nombre = "Juan Perez", NumeroDocumento = "12345678" });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Null(cliente);
        Assert.Equal("El email es obligatorio", mensaje);
    }

    [Fact]
    public async Task CreateCliente_ConEmailInvalido_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new ClienteRepository(context, TestDbContextFactory.Mapper);

        var (estado, _, mensaje) = await repo.CreateCliente(new CreateClientePayload
        { Nombre = "Juan Perez", NumeroDocumento = "12345678", Email = "no-es-email", Telefono = "999999999" });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Equal("El email no es válido", mensaje);
    }

    [Fact]
    public async Task CreateCliente_ConEmailYTelefono_Ok()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new ClienteRepository(context, TestDbContextFactory.Mapper);

        // Direccion en blanco: el mapeo AutoMapper hace Direccion.ToUpper() sin null-check (bug
        // preexistente, ajeno a esta validacion) y lanzaria NullReferenceException. UbigeoId en
        // null: el default del payload ("1000001") no existe en este contexto de prueba (no
        // siembra el catalogo de Ubigeo) y rompe la FK.
        var (estado, cliente, mensaje) = await repo.CreateCliente(new CreateClientePayload
        { Nombre = "Juan Perez", NumeroDocumento = "12345678", Email = "juan@correo.com", Telefono = "999999999", Direccion = "", UbigeoId = null! });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.NotNull(cliente);
    }
}
