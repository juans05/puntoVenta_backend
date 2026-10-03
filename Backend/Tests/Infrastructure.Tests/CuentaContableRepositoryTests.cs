using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class CuentaContableRepositoryTests
{
    // Nivel y ClaseCuenta se calculan del Codigo (ver "configuracion plan de cuentas ERPdocx.docx"):
    // solo codigos de 2/3/4/5/8 digitos son validos, y el Nivel/ClaseCuenta resultante no se recibe
    // del payload -- lo calcula el repositorio.
    [Theory]
    [InlineData("77", 1)]
    [InlineData("777", 2)]
    [InlineData("7777", 3)]
    [InlineData("77777", 4)]
    [InlineData("77777777", 5)]
    public async Task Crear_CalculaNivelYClaseCuentaSegunLargoDelCodigo(string codigo, int nivelEsperado)
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new CuentaContableRepository(context);

        var (estado, cuenta, mensaje) = await repo.Crear(new CrearCuentaContablePayload { Codigo = codigo, Nombre = "Prueba", Tipo = "Activo" });

        Assert.True(estado == Domain.Models.ServiceStatus.Ok, mensaje);
        Assert.Equal(nivelEsperado, cuenta!.Nivel);
        Assert.Equal(codigo[..2], cuenta.ClaseCuenta);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("77777777777")]
    [InlineData("7A")]
    public async Task Crear_ConCodigoDeLargoInvalido_FailedValidation(string codigo)
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new CuentaContableRepository(context);

        var (estado, _, mensaje) = await repo.Crear(new CrearCuentaContablePayload { Codigo = codigo, Nombre = "Prueba", Tipo = "Activo" });

        Assert.Equal(Domain.Models.ServiceStatus.FailedValidation, estado);
        Assert.NotEmpty(mensaje);
    }

    [Fact]
    public async Task Actualizar_ConCicloIndirectoDeCuentaPadre_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var repo = new CuentaContableRepository(context);

        // A (hija de 10, seed) -> B (hija de A). Intentar poner a A como hija de B cierra el ciclo.
        // Codigos de prueba: deben seguir la jerarquia de digitos del plan de cuentas (2/3/4/5/8).
        var padreId10 = (await context.CuentaContable.AsNoTracking().SingleAsync(c => c.Codigo == "10")).Id;
        var (estadoA, cuentaA, _) = await repo.Crear(new CrearCuentaContablePayload { Codigo = "999", Nombre = "A", Tipo = "Activo", CuentaPadreId = padreId10 });
        Assert.Equal(Domain.Models.ServiceStatus.Ok, estadoA);

        var (_, cuentaB, _) = await repo.Crear(new CrearCuentaContablePayload { Codigo = "9999", Nombre = "B", Tipo = "Activo", CuentaPadreId = cuentaA!.Id });

        var (estado, resultado, mensaje) = await repo.Actualizar(cuentaA.Id, new ActualizarCuentaContablePayload
        {
            Codigo = "999", Nombre = "A", Tipo = "Activo", CuentaPadreId = cuentaB!.Id, Estado = true
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

        var (_, cuenta, _) = await repo.Crear(new CrearCuentaContablePayload { Codigo = "6399", Nombre = "Gasto propio", Tipo = "Gasto" });

        var (estado, _, mensaje) = await repo.Actualizar(cuenta!.Id, new ActualizarCuentaContablePayload
        {
            Codigo = "6399", Nombre = "Gasto propio", Tipo = "Gasto", CuentaPadreId = cuenta.Id, Estado = true
        });

        Assert.Equal(Domain.Models.ServiceStatus.FailedValidation, estado);
        Assert.Contains("ciclo", mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
