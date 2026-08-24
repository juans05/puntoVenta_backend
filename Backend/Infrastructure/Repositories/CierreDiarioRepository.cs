using Application.Helper;
using Application.Interfaces.IRepository;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories;

public class CierreDiarioRepository : ICierreDiarioRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public CierreDiarioRepository(SpaContext context, IMapper mapper, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    public async Task<(ServiceStatus, ResumenDia?, string)> ResumenDia(int? sucursalId = null)
    {
        var user = _httpContextAccessor?.HttpContext?.User.FindFirstValue("username")?.ToUpper();
        sucursalId ??= _context.CurrentSucursalId;

        try
        {
            var resumen = await CalcularResumen(user, sucursalId);
            return (ServiceStatus.Ok, resumen, "Resumen del dia");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al calcular resumen -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    private async Task<ResumenDia> CalcularResumen(string? user, int? sucursalId)
    {
        var today = NowLocal().Date;

        var cajaAbierta = await _context.Caja.AsNoTracking()
            .Where(x => x.UsuarioCreacion == user && x.FechaCreacion.Date == today && x.FechaHoraCierre == null && x.SucursalId == sucursalId)
            .FirstOrDefaultAsync();

        var saldoInicial = cajaAbierta?.MontoInicio ?? 0;

        var ventasHoy = await _context.Pago.AsNoTracking()
            .Where(p => p.UsuarioCreacion == user && p.FechaCreacion.Date == today && p.CajaId == null)
            .SumAsync(p => (decimal?)p.Monto) ?? 0;

        var otrosIngresos = await _context.Ingreso.AsNoTracking()
            .Where(i => i.Estado == "CONFIRMADO" && i.FechaIngreso.Date == today && (i.SucursalId == null || i.SucursalId == sucursalId))
            .SumAsync(i => (decimal?)i.Monto) ?? 0;

        var gastos = await _context.Gasto.AsNoTracking()
            .Where(g => g.Estado == "CONFIRMADO" && g.FechaGasto.Date == today && (g.SucursalId == null || g.SucursalId == sucursalId))
            .SumAsync(g => (decimal?)g.Monto) ?? 0;

        var compras = await _context.Compra.AsNoTracking()
            .Where(c => c.Estado == "CONFIRMADO" && c.FechaCompra.Date == today && (c.SucursalId == null || c.SucursalId == sucursalId))
            .SumAsync(c => (decimal?)c.Total) ?? 0;

        var retiros = cajaAbierta != null
            ? await _context.Retiros.AsNoTracking().Where(r => r.CajaId == cajaAbierta.Id).SumAsync(r => (decimal?)r.Monto) ?? 0
            : 0;

        var ingresos = ventasHoy + otrosIngresos;
        var egresos = gastos + compras + retiros;
        var saldoEsperado = saldoInicial + ingresos - egresos;

        return new ResumenDia
        {
            SaldoInicial = saldoInicial,
            Ventas = ventasHoy,
            OtrosIngresos = otrosIngresos,
            Ingresos = ingresos,
            Gastos = gastos,
            Compras = compras,
            Retiros = retiros,
            Egresos = egresos,
            SaldoEsperado = saldoEsperado
        };
    }

    public async Task<(ServiceStatus, CierreDiarioDto?, string)> CerrarDia(CierreDiarioPayload payload, int? sucursalId = null)
    {
        var user = _httpContextAccessor?.HttpContext?.User.FindFirstValue("username")?.ToUpper();
        sucursalId ??= _context.CurrentSucursalId;
        var today = NowLocal().Date;

        try
        {
            var yaCerro = await _context.CierreDiario.AsNoTracking()
                .AnyAsync(c => c.UsuarioCreacion == user && c.FechaCierre.Date == today && c.SucursalId == sucursalId);

            if (yaCerro)
                return (ServiceStatus.FailedValidation, null, "El dia ya fue cerrado para este usuario");

            var resumen = await CalcularResumen(user, sucursalId);

            var cierre = new CierreDiario
            {
                FechaCierre = NowLocal(),
                SaldoInicial = resumen.SaldoInicial,
                Ingresos = resumen.Ingresos,
                Egresos = resumen.Egresos,
                SaldoEsperado = resumen.SaldoEsperado,
                SaldoReal = payload.SaldoReal,
                Diferencia = payload.SaldoReal - resumen.SaldoEsperado,
                Observaciones = payload.Observaciones
            };

            await _context.CierreDiario.AddAsync(cierre);
            await _context.SaveChangesAsync();

            var dto = await _context.CierreDiario.AsNoTracking()
                                    .ProjectTo<CierreDiarioDto>(_mapper.ConfigurationProvider)
                                    .FirstOrDefaultAsync(c => c.Id == cierre.Id);

            return (ServiceStatus.Ok, dto, "Cierre diario registrado correctamente");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al cerrar el dia -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<CierreDiarioDto>?, string)> ListarCierres(CierreDiarioQueryParams payload)
    {
        try
        {
            var query = _context.CierreDiario.AsNoTracking().AsQueryable();

            if (DateTime.TryParse(payload.StartDate, out var start))
                query = query.Where(c => c.FechaCierre.Date >= start.Date);

            if (DateTime.TryParse(payload.EndDate, out var end))
                query = query.Where(c => c.FechaCierre.Date <= end.Date);

            var lista = await query.OrderByDescending(c => c.Id)
                                   .ProjectTo<CierreDiarioDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            if (!lista.HasItems)
                return (ServiceStatus.NotFound, null, "No hay cierres para mostrar");

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar cierres -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}