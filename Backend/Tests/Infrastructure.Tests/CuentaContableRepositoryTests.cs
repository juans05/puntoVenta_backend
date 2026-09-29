using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class CuentaContableRepositoryTests
{
    [Fact]
    public async Task Actualizar_ConCicloIndirectoDeCuentaPadre_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new CuentaContableRepository(context);

        // A (hija de 10, seed) -> B (hija de A). Intentar poner a A como hija de B cierra el ciclo.
        var padreId10 = (await context.CuentaContable.AsNoTracking().SingleAsync(c => c.Codigo == "10")).Id;
        var (estadoA, cuentaA, _) = await repo.Crear(new CrearCuentaContablePayload { Codigo = "10.1", Nombre = "A", Tipo = "Activo", CuentaPadreId = padreId10 });
        Assert.Equal(Domain.Models.ServiceStatus.Ok, estadoA);

        var (_, cuentaB, _) = await repo.Crear(new CrearCuentaContablePayload { Codigo = "10.1.1", Nombre = "B", Tipo = "Activo", CuentaPadreId = cuentaA!.Id });

        var (estado, resultado, mensaje) = await repo.Actualizar(cuentaA.Id, new ActualizarCuentaContablePayload
        {
            Codigo = "10.1", Nombre = "A", Tipo = "Activo", CuentaPadreId = cuentaB!.Id, Estado = true
        });

        Assert.Equal(Domain.Models.ServiceStatus.FailedValidation, estado);
        Assert.Null(resultado);
        Assert.Contains("ciclo", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Actualizar_ConCuentaPropiaComoPadre_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new CuentaContableRepository(context);

        var (_, cuenta, _) = await repo.Crear(new CrearCuentaContablePayload { Codigo = "63.9", Nombre = "Gasto propio", Tipo = "Gasto" });

        var (estado, _, mensaje) = await repo.Actualizar(cuenta!.Id, new ActualizarCuentaContablePayload
        {
            Codigo = "63.9", Nombre = "Gasto propio", Tipo = "Gasto", CuentaPadreId = cuenta.Id, Estado = true
        });

        Assert.Equal(Domain.Models.ServiceStatus.FailedValidation, estado);
        Assert.Contains("ciclo", mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
