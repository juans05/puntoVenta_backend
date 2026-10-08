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
using Infrastructure.Common;
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
    private readonly IAsientoContableRepository _asientoContableRepository;

    public InventoryRepository(SpaContext context, IMapper mapper, IHttpContextAccessor? httpContextAccessor, IAsientoContableRepository asientoContableRepository)
    {
        _asientoContableRepository = asientoContableRepository;
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

    public async Task<(ServiceStatus, InventoryMovementDto?, string)> RegistrarMovimiento(int productoId, TipoMovimientoInventario tipo, int cantidad, string? referenciaTipo = null, int? referenciaId = null, int? sucursalId = null)
    {
        await _context.Database.BeginTransactionAsync();
        try
        {
            if (cantidad <= 0)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.FailedValidation, null, "La cantidad debe ser mayor a cero");
            }

            var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == productoId);

            if (producto == null)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.NotFound, null, $"No se encontro el producto {productoId}");
            }

            var delta = EsEntrada(tipo) ? cantidad : -cantidad;
            var ajuste = await StockSucursalHelper.Ajustar(_context, producto, sucursalId, delta);

            if (!ajuste.Ok)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.FailedValidation, null, ajuste.Error);
            }

            var movimiento = new InventoryMovement
            {
                ProductoId = producto.Id,
                SucursalId = ajuste.SucursalIdUsada,
                TipoMovimiento = (int)tipo,
                Cantidad = cantidad,
                StockAnterior = ajuste.StockAnteriorSucursal,
                StockPosterior = ajuste.StockPosteriorSucursal,
                ReferenciaTipo = referenciaTipo,
                ReferenciaId = referenciaId
            };

            await _context.InventoryMovement.AddAsync(movimiento);
            await _context.SaveChangesAsync();

            // Ajuste al costo contra 61 Variacion de existencias: entrada 20 / 61, salida 61 / 20.
            // ponytail: un solo par 20<->61 para sobrantes y faltantes; usar 659x/759x si contabilidad
            // quiere separar mermas como gasto.
            var costo = Math.Round((producto.CostoUnitario ?? 0) * cantidad, 2);
            if ((tipo is TipoMovimientoInventario.AjusteEntrada or TipoMovimientoInventario.AjusteSalida) && costo > 0)
            {
                var inventario = await _asientoContableRepository.CodigoCuenta(producto.CuentaInventarioDeMovimiento(tipo) ?? producto.CuentaInventarioId, "20");
                var entrada = tipo == TipoMovimientoInventario.AjusteEntrada;
                var (estadoAsiento, _, mensajeAsiento) = await _asientoContableRepository.Generar(
                    OrigenAsientoContable.AjusteInventario, movimiento.Id, $"Ajuste de {(entrada ? "entrada" : "salida")} - {producto.Nombre}",
                    new List<LineaAsientoContable>
                    {
                        new(inventario, entrada ? costo : 0, entrada ? 0 : costo),
                        new("61", entrada ? 0 : costo, entrada ? costo : 0),
                    });
                if (estadoAsiento != ServiceStatus.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, $"No se pudo generar el asiento contable -> {mensajeAsiento}");
                }
            }

            await _context.Database.CommitTransactionAsync();

            var dto = await _context.InventoryMovement.AsNoTracking()
                                        .Include(m => m.Producto)
                                        .ProjectTo<InventoryMovementDto>(_mapper.ConfigurationProvider)
                                        .FirstOrDefaultAsync(m => m.Id == movimiento.Id);

            return (ServiceStatus.Ok, dto, "Movimiento registrado correctamente");
        }
        catch (Exception ex)
        {
            await _context.Database.RollbackTransactionAsync();
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

        return await RegistrarMovimiento(payload.ProductoId, tipo, payload.Cantidad, "Ajuste", null, payload.SucursalId);
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