using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class CuentaIgvTests
{
    [Fact]
    public async Task CodigoCuentaIgv_UsaLaCuentaDelTipoGravado_YSinConfigurarUsa40111()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var gravado = new TipoIgv { Codigo = "10", Descripcion = "Gravado", AplicaPorcentajeImpuesto = true };
        var exonerado = new TipoIgv { Codigo = "20", Descripcion = "Exonerado", AplicaPorcentajeImpuesto = false };
        var cuenta = new CuentaContable { Codigo = "40112", Nombre = "IGV - Otra", Tipo = TipoCuentaContable.Pasivo };
        context.AddRange(gravado, exonerado, cuenta);
        await context.SaveChangesAsync();

        Assert.Equal("40111", await context.CodigoCuentaIgvAsync(new int?[] { gravado.Id }));

        context.TipoIgvCuenta.Add(new TipoIgvCuenta { TipoIgvId = gravado.Id, CuentaContableId = cuenta.Id });
        context.TipoIgvCuenta.Add(new TipoIgvCuenta { TipoIgvId = exonerado.Id, CuentaContableId = cuenta.Id });
        await context.SaveChangesAsync();

        Assert.Equal("40112", await context.CodigoCuentaIgvAsync(new int?[] { null, gravado.Id }));
        // Un tipo que no aplica IGV no define la cuenta del IGV.
        Assert.Equal("40111", await context.CodigoCuentaIgvAsync(new int?[] { exonerado.Id }));
        Assert.Equal("40111", await context.CodigoCuentaIgvAsync(new int?[] { null }));
    }
}
