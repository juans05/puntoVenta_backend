using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class AsientoContableRepositoryTests
{
    private static (AsientoContableRepository Repo, SpaContext Context, System.Data.Common.DbConnection Connection) Preparar()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        return (new AsientoContableRepository(context), context, connection);
    }

    [Fact]
    public async Task Generar_PartidaNoCuadra_EsRechazada()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;

        var (estado, asiento, mensaje) = await repo.Generar("Factura", 1, "Prueba",
            new List<LineaAsientoContable> { new("20", 100m, 0), new("42", 0, 90m) });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Null(asiento);
        Assert.Contains("no cuadra", mensaje);
    }

    [Fact]
    public async Task Generar_CuentaInexistente_EsRechazada()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;

        var (estado, _, mensaje) = await repo.Generar("Factura", 1, "Prueba",
            new List<LineaAsientoContable> { new("NOEXISTE", 100m, 0), new("42", 0, 100m) });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("NOEXISTE", mensaje);
    }

    [Fact]
    public async Task Generar_CreaAsientoConLineasYCuentasResueltas()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;

        var (estado, asiento, mensaje) = await repo.Generar("MovimientoInventario", 5, "Recepción REC-000001",
            new List<LineaAsientoContable> { new("20", 250m, 0), new("4211", 0, 250m) });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(EstadoAsientoContable.Emitido, asiento!.EstadoAsiento);
        Assert.Equal(2, asiento.Detalle.Count);
        Assert.Equal(250m, asiento.Detalle.Sum(d => d.Debe));
        Assert.Equal(250m, asiento.Detalle.Sum(d => d.Haber));
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "20" && d.Debe == 250m);
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "4211" && d.Haber == 250m);
    }

    [Fact]
    public async Task Reversar_MarcaOriginalAnuladoYCreaAsientoInverso()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await repo.Generar("MovimientoInventario", 5, "Recepción REC-000001",
            new List<LineaAsientoContable> { new("20", 250m, 0), new("4211", 0, 250m) });

        var (estado, reverso, mensaje) = await repo.Reversar("MovimientoInventario", 5);

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(EstadoAsientoContable.Emitido, reverso!.EstadoAsiento);
        Assert.Contains(reverso.Detalle, d => d.CuentaCodigo == "20" && d.Haber == 250m);
        Assert.Contains(reverso.Detalle, d => d.CuentaCodigo == "4211" && d.Debe == 250m);

        var original = context.AsientoContable.First(a => a.OrigenTipo == "MovimientoInventario" && a.OrigenId == 5 && a.Id != reverso.Id);
        Assert.Equal(EstadoAsientoContable.Anulado, original.EstadoAsiento);
    }

    [Fact]
    public async Task Reversar_SinAsientoActivo_EsNotFound()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;

        var (estado, _, mensaje) = await repo.Reversar("Factura", 999);

        Assert.Equal(ServiceStatus.NotFound, estado);
    }
}
