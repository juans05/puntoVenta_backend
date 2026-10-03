using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class CierreAnualRepositoryTests
{
    [Fact]
    public async Task Ejecutar_CierraResultadosYBalance_YGeneraAperturaCuadrada()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection; using var __ = context;
        var asientos = new AsientoContableRepository(context);
        var repo = new CierreAnualRepository(context);

        foreach (var (cod, tipo) in new[] { ("81", "Gasto"), ("85", "Patrimonio"), ("88111000", "Gasto"), ("89111000", "Patrimonio"), ("40171000", "Pasivo"), ("59111000", "Patrimonio"), ("59211000", "Patrimonio") })
            context.CuentaContable.Add(new CuentaContable { Codigo = cod, Nombre = cod, Tipo = tipo, Nivel = 1, ClaseCuenta = cod[..2], TenantId = context.CurrentTenantName });
        await context.SaveChangesAsync();
        var c81 = context.CuentaContable.AsTracking().First(c => c.Codigo == "81");
        var c85 = context.CuentaContable.AsTracking().First(c => c.Codigo == "85");
        foreach (var cod in new[] { "70", "63" }) context.CuentaContable.AsTracking().First(c => c.Codigo == cod).CuentaCierreId = c81.Id;
        foreach (var cod in new[] { "10", "40171000", "89111000" }) context.CuentaContable.AsTracking().First(c => c.Codigo == cod).CuentaCierreId = c85.Id;
        await context.SaveChangesAsync();

        await asientos.Generar("Venta", 1, "venta", new() { new("10", 1000m, 0), new("70", 0, 1000m) });
        await asientos.Generar("Factura", 1, "gasto", new() { new("63", 400m, 0), new("10", 0, 400m) });

        var anio = DateTime.UtcNow.AddHours(-5).Year;
        var payload = new CierreAnualPayload { Anio = anio, TasaImpuestoRenta = 10, ConfirmaDifCambio = true };
        var (estado, r, msg) = await repo.Ejecutar(payload);

        Assert.True(estado == ServiceStatus.Ok, msg);
        Assert.Equal(600m, r!.ResultadoAntesDeImpuestos);
        Assert.Equal(60m, r.ImpuestoRenta);
        Assert.Equal(540m, r.ResultadoNeto);
        Assert.Equal(5, r.Fases.Count);

        var detalle = await context.AsientoContableDetalle.Include(d => d.CuentaContable).Include(d => d.AsientoContable)
            .Where(d => d.AsientoContable!.OrigenTipo == "CierreAnual" || d.AsientoContable.OrigenTipo == "AperturaAnual").ToListAsync();
        Assert.Equal(detalle.Sum(d => d.Debe), detalle.Sum(d => d.Haber));
        Assert.Contains(detalle, d => d.CuentaContable!.Codigo == "59111000" && d.Haber == 540m && d.AsientoContable!.OrigenTipo == "AperturaAnual");

        var (estado2, _, _) = await repo.Ejecutar(payload);
        Assert.Equal(ServiceStatus.FailedValidation, estado2);
    }
}
