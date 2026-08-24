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

public class GastoRepository : IGastoRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public GastoRepository(SpaContext context, IMapper mapper, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    public async Task<(ServiceStatus, GastoDto?, string)> CrearGasto(CreateGastoPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Categoria))
            return (ServiceStatus.FailedValidation, null, "La categoría es obligatoria");

        if (payload.Monto <= 0)
            return (ServiceStatus.FailedValidation, null, "El monto debe ser mayor a 0");

        try
        {
            var gasto = new Gasto
            {
                Categoria = payload.Categoria,
                Descripcion = payload.Descripcion,
                Monto = payload.Monto,
                MetodoPagoId = payload.MetodoPagoId,
                Observacion = payload.Observacion,
                Estado = "CONFIRMADO",
                FechaGasto = payload.FechaGasto ?? NowLocal()
            };

            await _context.Gasto.AddAsync(gasto);
            await _context.SaveChangesAsync();

            var dto = await _context.Gasto.AsNoTracking()
                                    .Include(g => g.Metodopago)
                                    .ProjectTo<GastoDto>(_mapper.ConfigurationProvider)
                                    .FirstOrDefaultAsync(g => g.Id == gasto.Id);

            return (ServiceStatus.Ok, dto, "Gasto registrado correctamente");
        }
        catch (Exception e)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al registrar gasto -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, GastoDto?, string)> AnularGasto(int id)
    {
        var gasto = await _context.Gasto.AsTracking().FirstOrDefaultAsync(g => g.Id == id);

        if (gasto == null)
            return (ServiceStatus.NotFound, null, $"No se encontro el gasto {id}");

        if (gasto.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, null, "El gasto ya se encuentra anulado");

        try
        {
            gasto.Estado = "ANULADO";

            await _context.SaveChangesAsync();

            var dto = await _context.Gasto.AsNoTracking()
                                    .Include(g => g.Metodopago)
                                    .ProjectTo<GastoDto>(_mapper.ConfigurationProvider)
                                    .FirstOrDefaultAsync(g => g.Id == gasto.Id);

            return (ServiceStatus.Ok, dto, "Gasto eliminado correctamente");
        }
        catch (Exception e)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al eliminar gasto -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ListarCategorias()
    {
        try
        {
            var categorias = await _context.CategoriaGasto.AsNoTracking()
                                                    .OrderBy(c => c.Nombre)
                                                    .Select(c => new
                                                    {
                                                        id = c.Id,
                                                        value = c.Nombre,
                                                        estado = c.Estado
                                                    }).ToListAsync();

            return (ServiceStatus.Ok, categorias, "Success");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> CrearCategoria(CreateCategoriaGastoPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Nombre))
            return (ServiceStatus.FailedValidation, null, "El nombre de la categoría es obligatorio");

        try
        {
            var categoria = new CategoriaGasto { Nombre = payload.Nombre.Trim() };

            await _context.CategoriaGasto.AddAsync(categoria);
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, categoria, "Categoría registrada correctamente");
        }
        catch (Exception e)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al registrar categoría -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> CambiarEstadoCategoria(int id, bool estado)
    {
        var categoria = await _context.CategoriaGasto.AsTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (categoria == null)
            return (ServiceStatus.NotFound, $"No se encontró la categoría {id}");

        categoria.Estado = estado;
        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, "Success");
    }

    public async Task<(ServiceStatus, DataCollection<GastoDto>?, string)> ListarGastos(GastoQueryParams payload)
    {
        try
        {
            var query = _context.Gasto.AsNoTracking().Include(g => g.Metodopago).AsQueryable();

            if (!string.IsNullOrEmpty(payload.Categoria))
                query = query.Where(g => g.Categoria == payload.Categoria);

            if (DateTime.TryParse(payload.StartDate, out var start))
                query = query.Where(g => g.FechaGasto.Date >= start.Date);

            if (DateTime.TryParse(payload.EndDate, out var end))
                query = query.Where(g => g.FechaGasto.Date <= end.Date);

            if (!string.IsNullOrEmpty(payload.Value))
                query = query.Where(g => g.Descripcion.Contains(payload.Value) || g.Categoria.Contains(payload.Value));

            var lista = await query.OrderByDescending(g => g.Id)
                                   .ProjectTo<GastoDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            if (!lista.HasItems)
                return (ServiceStatus.NotFound, null, "No hay gastos para mostrar");

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar gastos -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}
