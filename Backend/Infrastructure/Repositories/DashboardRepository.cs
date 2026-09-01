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

            // "Hoy" en estos indicadores en realidad cubre el periodo elegido (start7..today, 1 dia
            // por defecto) para que Gastos/Compras que se registran hoy con fecha real backdateada
            // (ej. importados por Excel) se reflejen al ampliar el periodo, igual que el grafico de
            // ventas. Van por la fecha real de la transaccion (venta/gasto/compra), no de registro.
            var ventasHoy = await _context.ComprobanteCabecera.AsNoTracking()
                .Where(c => (c.FechaVenta ?? c.FechaCreacion).Date >= start7 && (c.FechaVenta ?? c.FechaCreacion).Date <= today && c.EstadoComprobante != EstatusComprobante.Anulado)
                .SumAsync(c => (decimal?)c.ValorTotal) ?? 0;

            var gastosHoy = await _context.Gasto.AsNoTracking()
                .Where(g => g.Estado == "CONFIRMADO" && g.FechaGasto.Date >= start7 && g.FechaGasto.Date <= today)
                .SumAsync(g => (decimal?)g.Monto) ?? 0;

            var comprasHoy = await _context.Compra.AsNoTracking()
                .Where(c => c.Estado == "CONFIRMADO" && c.FechaCompra.Date >= start7 && c.FechaCompra.Date <= today)
                .SumAsync(c => (decimal?)c.Total) ?? 0;

            var otrosIngresosHoy = await _context.Ingreso.AsNoTracking()
                .Where(i => i.Estado == "CONFIRMADO" && i.FechaIngreso.Date >= start7 && i.FechaIngreso.Date <= today)
                .SumAsync(i => (decimal?)i.Monto) ?? 0;

            // Costo real (CompraDetalle.CostoUnitario, guardado en Producto.CostoUnitario en cada compra),
            // no el precio de venta: usar Precio aquí hacía que costoVentasHoy ≈ ventasHoy y la utilidad
            // estimada saliera siempre negativa (≈ -gastosHoy), sin reflejar el margen real.
            var costoVentasHoy = await _context.ComprobanteDetalle.AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date >= start7 && (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date <= today && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .SumAsync(d => (decimal?)(d.Cantidad * (d.CostoReal ?? (d.Producto != null ? (d.Producto.CostoUnitario ?? 0) : 0)))) ?? 0;

            var saldoInicial = await _context.Caja.AsNoTracking()
                .Where(x => x.UsuarioCreacion == usernameHoy
                            && x.FechaCreacion.Date == today && x.FechaHoraCierre == null)
                .SumAsync(x => (decimal?)x.MontoInicio) ?? 0;

            // Saldo esperado en caja = efectivo que realmente entro/salio hoy, asi que va por
            // FechaCreacion (cuando se registro el movimiento) y no por la fecha de venta/gasto/
            // compra (que puede estar backdateada). Usar ventasHoy/gastosHoy/comprasHoy aqui
            // desalinea el saldo del efectivo real cuando alguien corrige esas fechas.
            var cobrosHoy = await _context.Pago.AsNoTracking()
                .Where(p => p.FechaCreacion.Date == today && p.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .SumAsync(p => (decimal?)p.Monto) ?? 0;

            var gastosRegistradosHoy = await _context.Gasto.AsNoTracking()
                .Where(g => g.Estado == "CONFIRMADO" && g.FechaCreacion.Date == today)
                .SumAsync(g => (decimal?)g.Monto) ?? 0;

            var comprasRegistradasHoy = await _context.Compra.AsNoTracking()
                .Where(c => c.Estado == "CONFIRMADO" && c.FechaCreacion.Date == today)
                .SumAsync(c => (decimal?)c.Total) ?? 0;

            var otrosIngresosRegistradosHoy = await _context.Ingreso.AsNoTracking()
                .Where(i => i.Estado == "CONFIRMADO" && i.FechaCreacion.Date == today)
                .SumAsync(i => (decimal?)i.Monto) ?? 0;

            var saldoEsperado = saldoInicial + cobrosHoy + otrosIngresosRegistradosHoy - gastosRegistradosHoy - comprasRegistradasHoy;

            var stockTotal = await _context.Producto.AsNoTracking()
                .Where(p => p.Estado)
                .SumAsync(p => (int?)p.Stock) ?? 0;

            var productosStockBajo = await _context.Producto.AsNoTracking()
                .CountAsync(p => p.Estado && p.StockMinimo.HasValue && p.Stock.HasValue && p.Stock < p.StockMinimo);

            var ventas7 = await _context.ComprobanteCabecera.AsNoTracking()
                .Where(c => (c.FechaVenta ?? c.FechaCreacion).Date >= start7 && (c.FechaVenta ?? c.FechaCreacion).Date <= today && c.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(c => (c.FechaVenta ?? c.FechaCreacion).Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.ValorTotal) })
                .ToListAsync();

            var compras7 = await _context.Compra.AsNoTracking()
                .Where(c => c.Estado == "CONFIRMADO" && c.FechaCompra.Date >= start7 && c.FechaCompra.Date <= today)
                .GroupBy(c => c.FechaCompra.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Total) })
                .ToListAsync();

            var gastos7 = await _context.Gasto.AsNoTracking()
                .Where(g => g.Estado == "CONFIRMADO" && g.FechaGasto.Date >= start7 && g.FechaGasto.Date <= today)
                .GroupBy(g => g.FechaGasto.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Monto) })
                .ToListAsync();

            var costoVentas7 = await _context.ComprobanteDetalle.AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date >= start7 && (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date <= today && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(d => (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Cantidad * (x.CostoReal ?? (x.Producto != null ? (x.Producto.CostoUnitario ?? 0) : 0))) })
                .ToListAsync();

            var tendenciaDiaria = Enumerable.Range(0, dias)
                .Select(offset =>
                {
                    var dia = start7.AddDays(offset);
                    return new TendenciaDiaDto
                    {
                        Fecha = dia.ToString("dd/MM"),
                        Ventas = ventas7.FirstOrDefault(v => v.Fecha.Date == dia)?.Total ?? 0,
                        Compras = compras7.FirstOrDefault(v => v.Fecha.Date == dia)?.Total ?? 0,
                        Gastos = gastos7.FirstOrDefault(v => v.Fecha.Date == dia)?.Total ?? 0,
                        CostoVentas = costoVentas7.FirstOrDefault(v => v.Fecha.Date == dia)?.Total ?? 0
                    };
                })
                .ToList();

            var gastosPorCategoria = await _context.Gasto.AsNoTracking()
                .Where(g => g.Estado == "CONFIRMADO" && g.FechaGasto.Date >= start7 && g.FechaGasto.Date <= today)
                .GroupBy(g => g.Categoria)
                .Select(g => new CategoriaMontoDto { Categoria = g.Key, Total = g.Sum(x => x.Monto) })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            // Distrito/TipoEnvio los llena el cajero al vender (ComprobantePayload.Distrito/TipoEnvio,
            // ver ModalPay), no son datos fijos del negocio. Vacios = venta local sin domicilio
            // registrado, no cuentan para el ranking de distritos.
            var ventasPorDistrito = await _context.ComprobanteCabecera.AsNoTracking()
                .Where(c => (c.FechaVenta ?? c.FechaCreacion).Date >= start7 && (c.FechaVenta ?? c.FechaCreacion).Date <= today
                            && c.EstadoComprobante != EstatusComprobante.Anulado
                            && c.Distrito != null && c.Distrito != "")
                .GroupBy(c => c.Distrito!)
                .Select(g => new DistritoVentaDto { Distrito = g.Key, Total = g.Sum(x => x.ValorTotal), Cantidad = g.Count() })
                .OrderByDescending(g => g.Total)
                .Take(8)
                .ToListAsync();

            var ventasPorTipoEnvio = await _context.ComprobanteCabecera.AsNoTracking()
                .Where(c => (c.FechaVenta ?? c.FechaCreacion).Date >= start7 && (c.FechaVenta ?? c.FechaCreacion).Date <= today
                            && c.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(c => c.TipoEnvio ?? "LOCAL")
                .Select(g => new CategoriaMontoDto { Categoria = g.Key, Total = g.Sum(x => x.ValorTotal) })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            var topProductos = await _context.ComprobanteDetalle.AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date >= start7 && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(d => new { d.ProductoId, Nombre = d.Producto != null ? d.Producto.Nombre : "N/A" })
                .Select(g => new ProductoTopDto
                {
                    ProductoId = g.Key.ProductoId,
                    Producto = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Total = g.Sum(x => x.ValorUnitarioTotal),
                    Costo = g.Sum(x => x.Cantidad * (x.CostoReal ?? (x.Producto != null ? (x.Producto.CostoUnitario ?? 0) : 0)))
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
                alertas.Add(dias == 1 ? "Los gastos de hoy superan las ventas" : $"Los gastos de los últimos {dias} días superan las ventas");

            var resumen = new DashboardResumenDto
            {
                VentasHoy = ventasHoy,
                GastosHoy = gastosHoy,
                ComprasHoy = comprasHoy,
                OtrosIngresosHoy = otrosIngresosHoy,
                CostoVentasHoy = costoVentasHoy,
                UtilidadEstimada = ventasHoy - costoVentasHoy - gastosHoy,
                FlujoCaja = ventasHoy - comprasHoy - gastosHoy,
                SaldoEsperado = saldoEsperado,
                StockTotal = stockTotal,
                ProductosStockBajo = productosStockBajo,
                TendenciaDiaria = tendenciaDiaria,
                ProductosMasVendidos = topProductos,
                GastosPorCategoria = gastosPorCategoria,
                VentasPorDistrito = ventasPorDistrito,
                VentasPorTipoEnvio = ventasPorTipoEnvio,
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
                .Where(d => (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date >= start.Date
                            && (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion).Date <= end.Date
                            && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                .GroupBy(d => new { d.ProductoId, Nombre = d.Producto != null ? d.Producto.Nombre : "N/A" })
                .Select(g => new ProductoTopDto
                {
                    ProductoId = g.Key.ProductoId,
                    Producto = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Total = g.Sum(x => x.ValorUnitarioTotal),
                    Costo = g.Sum(x => x.Cantidad * (x.CostoReal ?? (x.Producto != null ? (x.Producto.CostoUnitario ?? 0) : 0)))
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