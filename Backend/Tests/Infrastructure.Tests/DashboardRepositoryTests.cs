using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class DashboardRepositoryTests
{
    private static async Task<int> SeedTipoDocumentoVentaAsync(SpaContext context)
    {
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 1, Nombre = "Boleta" });
        await context.SaveChangesAsync();
        return 1;
    }

    private static async Task<int> SeedProductoAsync(SpaContext context, decimal costoUnitario)
    {
        var producto = new Producto { Nombre = "Producto test", Precio = 10m, CostoUnitario = costoUnitario, RestriccionEdad = 0 };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();
        return producto.Id;
    }

    private static async Task SeedVentaAsync(SpaContext context, int tipoDocumentoVentaId, int productoId, char estado, int cantidad, decimal valorUnitario)
    {
        var cabecera = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = tipoDocumentoVentaId,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = cantidad * valorUnitario,
            EstadoComprobante = estado,
        };
        context.ComprobanteCabecera.Add(cabecera);
        await context.SaveChangesAsync();

        context.ComprobanteDetalle.Add(new ComprobanteDetalle
        {
            ComprobanteCabeceraId = cabecera.Id,
            ProductoId = productoId,
            Cantidad = cantidad,
            ValorUnitario = valorUnitario,
            ValorUnitarioTotal = cantidad * valorUnitario,
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ReporteMargen_CalculaUtilidadYExcluyeVentasAnuladas()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var tipoDocId = await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, costoUnitario: 6m);

        // Venta valida: 2 unidades a 10 c/u = 20 venta, costo 12 -> utilidad 8
        await SeedVentaAsync(context, tipoDocId, productoId, EstatusComprobante.Creado, cantidad: 2, valorUnitario: 10m);

        // Venta anulada: no debe sumar ni a venta ni a costo
        await SeedVentaAsync(context, tipoDocId, productoId, EstatusComprobante.Anulado, cantidad: 100, valorUnitario: 10m);

        var repo = new DashboardRepository(context, httpContextAccessor: null);

        var today = DateTime.UtcNow.AddHours(-5).ToString("yyyy-MM-dd");
        var (estado, reporte, _) = await repo.ReporteMargen(today, today);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(reporte);

        Assert.Equal(20m, reporte!.TotalVentas);
        Assert.Equal(12m, reporte.TotalCosto);
        Assert.Equal(8m, reporte.TotalUtilidad);
        Assert.Equal(40m, reporte.MargenPorcentaje); // 8/20 * 100

        var linea = Assert.Single(reporte.Productos);
        Assert.Equal(productoId, linea.ProductoId);
        Assert.Equal(2, linea.Cantidad);
        Assert.Equal(8m, linea.Utilidad);
    }

    [Fact]
    public async Task ReporteMargen_FechaInvalida_RetornaFailedValidation()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var repo = new DashboardRepository(context, httpContextAccessor: null);

        var (estado, reporte, message) = await repo.ReporteMargen("no-es-una-fecha", null);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Null(reporte);
    }
}
