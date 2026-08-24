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

public class IngresoRepository : IIngresoRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public IngresoRepository(SpaContext context, IMapper mapper, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    public async Task<(ServiceStatus, IngresoDto?, string)> CrearIngreso(CreateIngresoPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Tipo))
            return (ServiceStatus.FailedValidation, null, "El tipo de ingreso es obligatorio");

        if (payload.Monto <= 0)
            return (ServiceStatus.FailedValidation, null, "El monto debe ser mayor a cero");

        try
        {
            var ingreso = new Ingreso
            {
                Tipo = payload.Tipo.Trim(),
                Monto = payload.Monto,
                MetodoPagoId = payload.MetodoPagoId,
                Descripcion = payload.Descripcion,
                FechaIngreso = payload.FechaIngreso ?? NowLocal()
            };

            await _context.Ingreso.AddAsync(ingreso);
            await _context.SaveChangesAsync();

            var dto = await _context.Ingreso.AsNoTracking()
                                    .Include(i => i.Metodopago)
                                    .ProjectTo<IngresoDto>(_mapper.ConfigurationProvider)
                                    .FirstOrDefaultAsync(i => i.Id == ingreso.Id);

            return (ServiceStatus.Ok, dto, "Ingreso registrado correctamente");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al registrar ingreso -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, IngresoDto?, string)> AnularIngreso(int id)
    {
        var ingreso = await _context.Ingreso.AsTracking().FirstOrDefaultAsync(i => i.Id == id);

        if (ingreso == null)
            return (ServiceStatus.NotFound, null, $"No se encontro el ingreso {id}");

        if (ingreso.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, null, "El ingreso ya se encuentra anulado");

        ingreso.Estado = "ANULADO";
        await _context.SaveChangesAsync();

        var (_, dto, _) = await ObtenerIngreso(id);

        return (ServiceStatus.Ok, dto, "Ingreso anulado correctamente");
    }

    public async Task<(ServiceStatus, DataCollection<IngresoDto>?, string)> ListarIngresos(IngresoQueryParams payload)
    {
        try
        {
            var query = _context.Ingreso.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(payload.Tipo))
                query = query.Where(i => i.Tipo == payload.Tipo);

            if (DateTime.TryParse(payload.StartDate, out var start))
                query = query.Where(i => i.FechaIngreso.Date >= start.Date);

            if (DateTime.TryParse(payload.EndDate, out var end))
                query = query.Where(i => i.FechaIngreso.Date <= end.Date);

            if (!string.IsNullOrEmpty(payload.Value))
                query = query.Where(i => i.Tipo.Contains(payload.Value) || (i.Descripcion != null && i.Descripcion.Contains(payload.Value)));

            var lista = await query.OrderByDescending(i => i.Id)
                                   .ProjectTo<IngresoDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            if (!lista.HasItems)
                return (ServiceStatus.NotFound, null, "No hay ingresos para mostrar");

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar ingresos -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    private async Task<(ServiceStatus, IngresoDto?, string)> ObtenerIngreso(int id)
    {
        var dto = await _context.Ingreso.AsNoTracking()
                                .Include(i => i.Metodopago)
                                .ProjectTo<IngresoDto>(_mapper.ConfigurationProvider)
                                .FirstOrDefaultAsync(i => i.Id == id);

        if (dto == null)
            return (ServiceStatus.NotFound, null, $"No se encontro el ingreso {id}");

        return (ServiceStatus.Ok, dto, "Success");
    }
}