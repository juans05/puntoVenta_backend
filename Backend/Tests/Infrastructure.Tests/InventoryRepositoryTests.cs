using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class InventoryRepositoryTests
{
    // Ajustes al costo contra 61: entrada 20/61, salida 61/20.
    [Theory]
    [InlineData(TipoMovimientoInventario.AjusteEntrada, "20", "61")]
    [InlineData(TipoMovimientoInventario.AjusteSalida, "61", "20")]
    public async Task AjustarStock_GeneraAsientoContra61(TipoMovimientoInventario tipo, string cuentaDebe, string cuentaHaber)
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        var producto = new Producto { Nombre = "Ajuste test", Precio = 10, Stock = 10, CostoUnitario = 5m, RestriccionEdad = 0 };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();
        var repo = new InventoryRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new AsientoContableRepository(context));

        var (estado, _, mensaje) = await repo.AjustarStock(new CreateAjusteInventarioPayload { ProductoId = producto.Id, TipoMovimiento = (int)tipo, Cantidad = 2 });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        var asiento = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == OrigenAsientoContable.AjusteInventario);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == cuentaDebe && d.Debe == 10m); // 2 x 5
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == cuentaHaber && d.Haber == 10m);
    }
}
