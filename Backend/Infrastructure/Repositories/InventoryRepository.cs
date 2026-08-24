using Application.Helper;
using Application.Interfaces.IRepository;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public InventoryRepository(SpaContext context, IMapper mapper, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    private static bool EsEntrada(TipoMovimientoInventario tipo) =>
        tipo is TipoMovimientoInventario.Compra
            or TipoMovimientoInventario.AjusteEntrada
            or TipoMovimientoInventario.DevolucionVenta;

    public async Task<(ServiceStatus, InventoryMovementDto?, string)> RegistrarMovimiento(int productoId, TipoMovimientoInventario tipo, int cantidad, string? referenciaTipo = null, int? referenciaId = null)
    {
        try
        {
            if (cantidad <= 0)
                return (ServiceStatus.FailedValidation, null, "La cantidad debe ser mayor a cero");

            var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == productoId);

            if (producto == null)
                return (ServiceStatus.NotFound, null, $"No se encontro el producto {productoId}");

            var stockAnterior = producto.Stock ?? 0;
            var stockNuevo = EsEntrada(tipo) ? stockAnterior + cantidad : stockAnterior - cantidad;

            if (stockNuevo < 0)
                return (ServiceStatus.FailedValidation, null, $"Stock insuficiente para el producto {producto.Nombre}");

            producto.Stock = stockNuevo;

            var movimiento = new InventoryMovement
            {
                ProductoId = producto.Id,
                TipoMovimiento = (int)tipo,
                Cantidad = cantidad,
                StockAnterior = stockAnterior,
                StockPosterior = stockNuevo,
                ReferenciaTipo = referenciaTipo,
                ReferenciaId = referenciaId
            };

            await _context.InventoryMovement.AddAsync(movimiento);
            await _context.SaveChangesAsync();

            var dto = await _context.InventoryMovement.AsNoTracking()
                                        .Include(m => m.Producto)
                                        .ProjectTo<InventoryMovementDto>(_mapper.ConfigurationProvider)
                                        .FirstOrDefaultAsync(m => m.Id == movimiento.Id);

            return (ServiceStatus.Ok, dto, "Movimiento registrado correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al registrar movimiento -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, InventoryMovementDto?, string)> AjustarStock(CreateAjusteInventarioPayload payload)
    {
        if (!Enum.IsDefined(typeof(TipoMovimientoInventario), payload.TipoMovimiento))
            return (ServiceStatus.FailedValidation, null, "Tipo de movimiento invalido");

        var tipo = (TipoMovimientoInventario)payload.TipoMovimiento;

        if (tipo != TipoMovimientoInventario.AjusteEntrada && tipo != TipoMovimientoInventario.AjusteSalida)
            return (ServiceStatus.FailedValidation, null, "Solo se permiten ajustes de entrada o salida");

        if (payload.Cantidad <= 0)
            return (ServiceStatus.FailedValidation, null, "La cantidad debe ser mayor a cero");

        return await RegistrarMovimiento(payload.ProductoId, tipo, payload.Cantidad, "Ajuste", null);
    }

    public async Task<(ServiceStatus, DataCollection<InventoryMovementDto>?, string)> ListarMovimientos(InventoryMovementQuery payload)
    {
        try
        {
            var query = _context.InventoryMovement.AsNoTracking().Include(m => m.Producto).AsQueryable();

            if (payload.ProductoId.HasValue)
                query = query.Where(m => m.ProductoId == payload.ProductoId);

            if (payload.TipoMovimiento.HasValue)
                query = query.Where(m => m.TipoMovimiento == payload.TipoMovimiento);

            if (!string.IsNullOrEmpty(payload.Fecha) && DateTime.TryParse(payload.Fecha, out var fecha))
                query = query.Where(m => m.FechaCreacion.Date == fecha.Date);

            var lista = await query.OrderByDescending(m => m.Id)
                                   .ProjectTo<InventoryMovementDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            if (!lista.HasItems)
                return (ServiceStatus.NotFound, null, "No hay movimientos para mostrar");

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar movimientos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}