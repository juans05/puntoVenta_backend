using Application.Helper;
using Application.Interfaces.IRepository;
using Domain.DTO;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly SpaContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public DashboardRepository(SpaContext context, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    public async Task<(ServiceStatus, DashboardResumenDto?, string)> Resumen(int dias = 7)
    {
        try
        {
            dias = Math.Clamp(dias, 1, 365);

            var today = NowLocal().Date;
            var start7 = today.AddDays(-(dias - 1));
            var usernameHoy = _httpContextAccessor?.HttpContext?.User.FindFirstValue("username")?.ToUpper();

            var ventasHoy = await _context.ComprobanteCabecera.AsNoTracking()
                .Where(c => c.FechaCreacion.Date == today && c.EstadoComprobante != EstatusComprobante.Anulado)
                .SumAsync(c => (decimal?)c.ValorTotal) ?? 0;

            var gastosHoy = await _context.Gasto.AsNoTracking()
                .Where(g => g.Estado == "CONFIRMADO" && g.FechaGasto.Date == today)
                .SumAsync(g => (decimal?)g.Monto) ?? 0;

            var comprasHoy = await _context.Compra.AsNoTracking()
                .Where(c => c.Estado == "CONFIRMADO" && c.FechaCompra.Date == today)
                .SumAsync(c => (decimal?)c.Total) ?? 0;

            var otrosIngresosHoy = await _context.Ingreso.AsNoTracking()
                .Where(i => i.Estado == "CONFIRMADO" && i.FechaIngreso.Date == today)
                .SumAsync(i => (decimal?)i.Monto) ?? 0;

            // Costo real (CompraDetalle.CostoUnitario, guardado en Producto.CostoUnitario en cada compra),
            // no el precio de venta: usar Precio aquí hacía que costoVentasHoy ≈ ventasHoy y la utilidad
            // estimada saliera siempre negativa (≈ -gastosHoy), sin reflejar el margen real.
            var costoVentasHoy = await _context.ComprobanteDetalle.AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => d.ComprobanteCabecera.FechaCreacion.Date == today && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .SumAsync(d => (decimal?)(d.Cantidad * (d.Producto != null ? (d.Producto.CostoUnitario ?? 0) : 0))) ?? 0;

            var saldoInicial = await _context.Caja.AsNoTracking()
                .Where(x => x.UsuarioCreacion == usernameHoy
                            && x.FechaCreacion.Date == today && x.FechaHoraCierre == null)
                .SumAsync(x => (decimal?)x.MontoInicio) ?? 0;

            var saldoEsperado = saldoInicial + ventasHoy + otrosIngresosHoy - gastosHoy - comprasHoy;

            var stockTotal = await _context.Producto.AsNoTracking()
                .Where(p => p.Estado)
                .SumAsync(p => (int?)p.Stock) ?? 0;

            var productosStockBajo = await _context.Producto.AsNoTracking()
                .CountAsync(p => p.Estado && p.StockMinimo.HasValue && p.Stock.HasValue && p.Stock < p.StockMinimo);

            var ventas7 = await _context.ComprobanteCabecera.AsNoTracking()
                .Where(c => c.FechaCreacion.Date >= start7 && c.FechaCreacion.Date <= today && c.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(c => c.FechaCreacion.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.ValorTotal) })
                .OrderBy(g => g.Fecha)
                .ToListAsync();

            var ventasUltimos7Dias = Enumerable.Range(0, dias)
                .Select(offset =>
                {
                    var dia = start7.AddDays(offset);
                    var registro = ventas7.FirstOrDefault(v => v.Fecha.Date == dia);
                    return new VentaDiaDto
                    {
                        Fecha = dia.ToString("dd/MM"),
                        Total = registro?.Total ?? 0
                    };
                })
                .ToList();

            var topProductos = await _context.ComprobanteDetalle.AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => d.ComprobanteCabecera.FechaCreacion.Date >= start7 && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(d => new { d.ProductoId, Nombre = d.Producto != null ? d.Producto.Nombre : "N/A" })
                .Select(g => new ProductoTopDto
                {
                    ProductoId = g.Key.ProductoId,
                    Producto = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Total = g.Sum(x => x.ValorUnitarioTotal),
                    Costo = g.Sum(x => x.Cantidad * (x.Producto != null ? (x.Producto.CostoUnitario ?? 0) : 0))
                })
                .OrderByDescending(p => p.Cantidad)
                .Take(5)
                .ToListAsync();

            var alertas = new List<string>();

            if (productosStockBajo > 0)
                alertas.Add($"Hay {productosStockBajo} producto(s) con stock bajo");

            var cajaAbierta = await _context.Caja.AsNoTracking()
                .AnyAsync(x => x.UsuarioCreacion == usernameHoy
                            && x.FechaCreacion.Date == today && x.FechaHoraCierre == null);

            if (cajaAbierta)
                alertas.Add("Tienes la caja abierta pendiente de cierre");

            if (gastosHoy > ventasHoy && ventasHoy > 0)
                alertas.Add("Los gastos de hoy superan las ventas");

            var resumen = new DashboardResumenDto
            {
                VentasHoy = ventasHoy,
                GastosHoy = gastosHoy,
                ComprasHoy = comprasHoy,
                OtrosIngresosHoy = otrosIngresosHoy,
                CostoVentasHoy = costoVentasHoy,
                UtilidadEstimada = ventasHoy - costoVentasHoy - gastosHoy,
                SaldoEsperado = saldoEsperado,
                StockTotal = stockTotal,
                ProductosStockBajo = productosStockBajo,
                VentasUltimos7Dias = ventasUltimos7Dias,
                ProductosMasVendidos = topProductos,
                Alertas = alertas
            };

            return (ServiceStatus.Ok, resumen, "Resumen del dashboard");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar dashboard -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, ReporteMargenDto?, string)> ReporteMargen(string? startDate, string? endDate)
    {
        try
        {
            var today = NowLocal().Date;

            DateTime start = today.AddDays(-29); // por defecto, ultimos 30 dias
            DateTime end = today;

            if (!string.IsNullOrEmpty(startDate) && !DateTime.TryParse(startDate, out start))
                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {startDate}");

            if (!string.IsNullOrEmpty(endDate) && !DateTime.TryParse(endDate, out end))
                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {endDate}");

            var productos = await _context.ComprobanteDetalle.AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => d.ComprobanteCabecera.FechaCreacion.Date >= start.Date
                            && d.ComprobanteCabecera.FechaCreacion.Date <= end.Date
                            && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(d => new { d.ProductoId, Nombre = d.Producto != null ? d.Producto.Nombre : "N/A" })
                .Select(g => new ProductoTopDto
                {
                    ProductoId = g.Key.ProductoId,
                    Producto = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Total = g.Sum(x => x.ValorUnitarioTotal),
                    Costo = g.Sum(x => x.Cantidad * (x.Producto != null ? (x.Producto.CostoUnitario ?? 0) : 0))
                })
                .OrderByDescending(p => p.Total)
                .ToListAsync();

            var reporte = new ReporteMargenDto
            {
                FechaInicio = start.ToString("dd/MM/yyyy"),
                FechaFin = end.ToString("dd/MM/yyyy"),
                TotalVentas = productos.Sum(p => p.Total),
                TotalCosto = productos.Sum(p => p.Costo),
                Productos = productos
            };

            return (ServiceStatus.Ok, reporte, "Reporte de margen por producto");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar reporte de margen -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}