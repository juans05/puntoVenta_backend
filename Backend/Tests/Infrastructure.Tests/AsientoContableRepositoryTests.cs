using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class AsientoContableRepositoryTests
{
    private static (AsientoContableRepository Repo, SpaContext Context, System.Data.Common.DbConnection Connection) Preparar()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        return (new AsientoContableRepository(context), context, connection);
    }

    // Gasto 63 con destino 60% -> 94 y 40% -> 95, ambos contra 79: se agregan los pares 9X/79 y
    // una copia (GenerarBasadoEn, ej. nota) no los vuelve a duplicar.
    [Fact]
    public async Task Generar_CuentaDeGastoConDestino_AgregaLineas9XContra79()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var c94 = new CuentaContable { Codigo = "94111000", Nombre = "Gastos administrativos", Tipo = TipoCuentaContable.Gasto, Nivel = 5, ClaseCuenta = "94" };
        var c95 = new CuentaContable { Codigo = "95111000", Nombre = "Gastos de ventas", Tipo = TipoCuentaContable.Gasto, Nivel = 5, ClaseCuenta = "95" };
        var c79 = new CuentaContable { Codigo = "79111000", Nombre = "Cargas imputables", Tipo = TipoCuentaContable.Ingreso, Nivel = 5, ClaseCuenta = "79" };
        context.CuentaContable.AddRange(c94, c95, c79);
        await context.SaveChangesAsync();
        context.CuentaContable.Add(new CuentaContable
        {
            Codigo = "63110000", Nombre = "Transporte", Tipo = TipoCuentaContable.Gasto, Nivel = 5, ClaseCuenta = "63", Destino = true,
            CuentaCargo1Id = c94.Id, CuentaAbono1Id = c79.Id, PorcentajeDestino1 = 60,
            CuentaCargo2Id = c95.Id, CuentaAbono2Id = c79.Id, PorcentajeDestino2 = 40,
        });
        await context.SaveChangesAsync();

        var (estado, asiento, mensaje) = await repo.Generar("Factura", 1, "Flete",
            new List<LineaAsientoContable> { new("63110000", 100m, 0), new("42", 0, 100m) });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(asiento!.Detalle.Sum(d => d.Debe), asiento.Detalle.Sum(d => d.Haber));
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "94111000" && d.Debe == 60m);
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "95111000" && d.Debe == 40m);
        Assert.Equal(100m, asiento.Detalle.Where(d => d.CuentaCodigo == "79111000").Sum(d => d.Haber));

        var (_, resultados, _) = await repo.ObtenerEstadoResultados(null, null);
        Assert.Equal(100m, resultados!.TotalGastos); // solo el 63; 94/95 y 79 no inflan el reporte
        Assert.Equal(0m, resultados.TotalIngresos);

        var (_, copia, _) = await repo.GenerarBasadoEn("Factura", 1, invertido: true, "NotaCompra", 1, "Nota");
        Assert.Equal(asiento.Detalle.Count, copia!.Detalle.Count);
        Assert.Contains(copia.Detalle, d => d.CuentaCodigo == "94111000" && d.Haber == 60m);
    }

    // Campos de la linea (tercero, CC1/CC2, descripcion) y fechas se guardan y el reverso los conserva;
    // una 9X elegida a mano en la linea genera el destino al 100% contra 79.
    [Fact]
    public async Task Generar_ConCamposAnaliticosYDestinoManual_LosGuardaYElReversoLosConserva()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        var c94 = new CuentaContable { Codigo = "94111000", Nombre = "Gastos administrativos", Tipo = TipoCuentaContable.Gasto, Nivel = 5, ClaseCuenta = "94" };
        var c79 = new CuentaContable { Codigo = "79", Nombre = "Cargas imputables", Tipo = TipoCuentaContable.Ingreso };
        var area = new CentroCosto { Nombre = "Administracion" };
        var proyecto = new CentroCosto { Nombre = "Proyecto A" };
        context.AddRange(c94, c79, area, proyecto);
        await context.SaveChangesAsync();
        var vence = new DateTime(2026, 11, 30);

        var (estado, asiento, mensaje) = await repo.Generar("Manual", 1, "Alquiler",
            new List<LineaAsientoContable>
            {
                new("63", 1500m, 0) { CentroCosto1Id = area.Id, CentroCosto2Id = proyecto.Id, CuentaDestinoId = c94.Id, Descripcion = "Alquiler de oficinas" },
                new("42", 0, 1500m) { CuentaAsociada = "20123456789" },
            }, fechaDocumento: new DateTime(2026, 10, 1), fechaVencimiento: vence);

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(vence, asiento!.FechaVencimiento);
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "63" && d.CentroCosto1 == "Administracion" && d.CentroCosto2 == "Proyecto A"
            && d.CuentaDestino == "94111000" && d.Descripcion == "Alquiler de oficinas");
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "42" && d.CuentaAsociada == "20123456789");
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "94111000" && d.Debe == 1500m);
        Assert.Contains(asiento.Detalle, d => d.CuentaCodigo == "79" && d.Haber == 1500m);

        var (_, reverso, _) = await repo.Reversar("Manual", 1);
        Assert.Equal(vence, reverso!.FechaVencimiento);
        Assert.Contains(reverso.Detalle, d => d.CuentaCodigo == "42" && d.Debe == 1500m && d.CuentaAsociada == "20123456789");
    }

    [Fact]
    public async Task Generar_CentroCostoOCuentaDestinoInvalidos_SonRechazados()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;

        var (e1, _, m1) = await repo.Generar("Manual", 1, "x",
            new List<LineaAsientoContable> { new("63", 10m, 0) { CentroCosto1Id = 9999 }, new("42", 0, 10m) });
        Assert.Equal(ServiceStatus.FailedValidation, e1);
        Assert.Contains("centro de costo", m1);

        var cuenta42Id = await context.CuentaContable.Where(c => c.Codigo == "42").Select(c => c.Id).SingleAsync();
        var (e2, _, m2) = await repo.Generar("Manual", 1, "x",
            new List<LineaAsientoContable> { new("63", 10m, 0) { CuentaDestinoId = cuenta42Id }, new("42", 0, 10m) });
        Assert.Equal(ServiceStatus.FailedValidation, e2);
        Assert.Contains("clase 9", m2);
    }

    private static async Task SeedCuentasHojaAsync(SpaContext context, string modoCentroCosto = ModoCentroCostoCuenta.Ninguno)
    {
        context.CuentaContable.AddRange(
            new CuentaContable { Codigo = "63111000", Nombre = "Alquileres", Tipo = TipoCuentaContable.Gasto, Nivel = 5, ClaseCuenta = "63", ModoCentroCosto = modoCentroCosto },
            new CuentaContable { Codigo = "42121000", Nombre = "Facturas por pagar", Tipo = TipoCuentaContable.Pasivo, Nivel = 5, ClaseCuenta = "42" });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CrearManual_Cuadrado_SeRegistraYAnularLoReversaUnaSolaVez()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await SeedCuentasHojaAsync(context);

        var (estado, asiento, mensaje) = await repo.CrearManual(new CrearAsientoManualPayload
        {
            Fecha = new DateTime(2026, 10, 5),
            Glosa = "Alquiler octubre",
            Lineas = new() { new("63111000", 1500m, 0) { Descripcion = "Alquiler de oficinas" }, new("42121000", 0, 1500m), new("42121000", 0, 0) },
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(OrigenAsientoContable.Manual, asiento!.OrigenTipo);
        Assert.Equal(asiento.Id, asiento.OrigenId);
        Assert.Equal(new DateTime(2026, 10, 5), asiento.Fecha);
        Assert.Equal(2, asiento.Detalle.Count); // la linea en cero se descarta

        var (e1, _, m1) = await repo.AnularManual(asiento.Id);
        Assert.True(e1 == ServiceStatus.Ok, m1);
        var (e2, _, m2) = await repo.AnularManual(asiento.Id);
        Assert.Equal(ServiceStatus.FailedValidation, e2);
        Assert.Contains("ya está anulado", m2);
    }

    [Theory]
    [InlineData("63", "Alquiler", "8 dígitos")]                // cuenta que no es de ultimo nivel
    [InlineData("63111000", "", "descripción")]                // sin glosa
    [InlineData("63111000", "Alquiler", "centro de costos")]   // la cuenta exige CC1
    public async Task CrearManual_Invalido_EsRechazado(string cuenta, string glosa, string error)
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await SeedCuentasHojaAsync(context, ModoCentroCostoCuenta.Obligatorio);

        var (estado, _, mensaje) = await repo.CrearManual(new CrearAsientoManualPayload
        {
            Glosa = glosa,
            Lineas = new() { new(cuenta, 100m, 0), new("42121000", 0, 100m) },
        });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains(error, mensaje);
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

    [Fact]
    public async Task ObtenerEstadoResultados_SumaIngresosYGastosPorCuenta()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await repo.Generar("Venta", 1, "Venta", new List<LineaAsientoContable> { new("10", 118m, 0), new("70", 0, 100m), new("40111", 0, 18m) });
        await repo.Generar("Venta", 1, "Costo", new List<LineaAsientoContable> { new("69", 60m, 0), new("20", 0, 60m) });

        var (estado, resultados, mensaje) = await repo.ObtenerEstadoResultados(null, null);

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(100m, Assert.Single(resultados!.Ingresos).Monto);
        Assert.Equal(60m, Assert.Single(resultados.Gastos).Monto);
        Assert.Equal(100m, resultados.TotalIngresos);
        Assert.Equal(60m, resultados.TotalGastos);
        Assert.Equal(40m, resultados.UtilidadNeta);
    }

    [Fact]
    public async Task ObtenerBalanceGeneral_ActivoCuadraContraPasivoMasPatrimonioMasResultado()
    {
        var (repo, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await repo.Generar("Venta", 1, "Venta", new List<LineaAsientoContable> { new("10", 118m, 0), new("70", 0, 100m), new("40111", 0, 18m) });
        await repo.Generar("Venta", 1, "Costo", new List<LineaAsientoContable> { new("69", 60m, 0), new("20", 0, 60m) });

        var (estado, balance, mensaje) = await repo.ObtenerBalanceGeneral(DateTime.UtcNow.AddHours(-5));

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(40m, balance!.ResultadoDelEjercicio);
        Assert.Equal(balance.TotalActivo, balance.TotalPasivoYPatrimonio); // identidad contable: Activo = Pasivo + Patrimonio + Resultado
        Assert.Equal(58m, balance.TotalActivo); // 118 (10, Debe) - 60 (20, Haber)
    }
}
