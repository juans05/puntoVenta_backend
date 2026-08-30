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

public class CompraRepository : ICompraRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public CompraRepository(SpaContext context, IMapper mapper, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    private async Task<string> GenerarNumeroCompra()
    {
        var count = await _context.Compra.CountAsync();
        return $"C-{(count + 1).ToString().PadLeft(6, '0')}";
    }

    public async Task<(ServiceStatus, CompraDto?, string)> CrearCompra(CreateCompraPayload payload)
    {
        if (payload.Detalle == null || payload.Detalle.Count == 0)
            return (ServiceStatus.FailedValidation, null, "La compra debe incluir al menos un producto");

        await _context.Database.BeginTransactionAsync();

        try
        {
            var total = payload.Detalle.Sum(d => d.Cantidad * d.CostoUnitario);

            var compra = new Compra
            {
                NumeroCompra = await GenerarNumeroCompra(),
                ProveedorId = payload.ProveedorId,
                Total = total,
                MetodoPagoId = payload.MetodoPagoId,
                Observacion = payload.Observacion,
                Estado = "CONFIRMADO",
                FechaCompra = payload.FechaCompra ?? NowLocal()
            };

            await _context.Compra.AddAsync(compra);
            await _context.SaveChangesAsync();

            var detalle = payload.Detalle.Select(d => new CompraDetalle
            {
                CompraId = compra.Id,
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                CostoUnitario = d.CostoUnitario
            }).ToList();

            await _context.CompraDetalle.AddRangeAsync(detalle);
            await _context.SaveChangesAsync();

            foreach (var item in detalle)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null)
                    return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                var stockAnterior = producto.Stock ?? 0;
                producto.Stock = stockAnterior + item.Cantidad;
                producto.CostoUnitario = item.CostoUnitario;

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = producto.Stock.Value,
                    ReferenciaTipo = "Compra",
                    ReferenciaId = compra.Id
                });
            }

            await _context.SaveChangesAsync();

            await _context.Database.CommitTransactionAsync();

            var (_, dto, _) = await ObtenerCompra(compra.Id);

            return (ServiceStatus.Ok, dto, "Compra registrada correctamente");
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al registrar compra -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, CompraDto?, string)> AnularCompra(int id)
    {
        var compra = await _context.Compra.AsTracking()
                                    .Include(c => c.CompraDetalles)
                                    .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            return (ServiceStatus.NotFound, null, $"No se encontro la compra {id}");

        if (compra.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, null, "La compra ya se encuentra anulada");

        await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var item in compra.CompraDetalles)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null) continue;

                var stockAnterior = producto.Stock ?? 0;
                var stockNuevo = stockAnterior - item.Cantidad;

                if (stockNuevo < 0)
                    return (ServiceStatus.FailedValidation, null, $"Stock insuficiente para revertir la compra del producto {producto.Nombre}");

                producto.Stock = stockNuevo;

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = stockNuevo,
                    ReferenciaTipo = "CompraAnulada",
                    ReferenciaId = compra.Id
                });
            }

            compra.Estado = "ANULADO";

            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();

            var (_, dto, _) = await ObtenerCompra(compra.Id);

            return (ServiceStatus.Ok, dto, "Compra anulada correctamente");
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al anular compra -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<CompraDto>?, string)> ListarCompras(CompraQueryParams payload)
    {
        try
        {
            var query = _context.Compra.AsNoTracking().Where(c => c.Estado != "ANULADO").AsQueryable();

            if (payload.ProveedorId.HasValue)
                query = query.Where(c => c.ProveedorId == payload.ProveedorId);

            if (DateTime.TryParse(payload.StartDate, out var start))
                query = query.Where(c => c.FechaCompra.Date >= start.Date);

            if (DateTime.TryParse(payload.EndDate, out var end))
                query = query.Where(c => c.FechaCompra.Date <= end.Date);

            if (!string.IsNullOrEmpty(payload.Value))
                query = query.Where(c => c.NumeroCompra.Contains(payload.Value) ||
                                        (c.Proveedor != null && c.Proveedor.Nombre.Contains(payload.Value)));

            var lista = await query.OrderByDescending(c => c.Id)
                                   .ProjectTo<CompraDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            if (!lista.HasItems)
                return (ServiceStatus.NotFound, null, "No hay compras para mostrar");

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar compras -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, CompraDto?, string)> ObtenerCompra(int id)
    {
        var dto = await _context.Compra.AsNoTracking()
                                .Include(c => c.Proveedor)
                                .Include(c => c.Metodopago)
                                .Include(c => c.CompraDetalles)
                                    .ThenInclude(d => d.Producto)
                                .ProjectTo<CompraDto>(_mapper.ConfigurationProvider)
                                .FirstOrDefaultAsync(c => c.Id == id);

        if (dto == null)
            return (ServiceStatus.NotFound, null, $"No se encontro la compra {id}");

        return (ServiceStatus.Ok, dto, "Success");
    }

    public async Task<(ServiceStatus, string)> ActualizarFechaCompra(int id, DateTime fecha)
    {
        var compra = await _context.Compra.AsTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            return (ServiceStatus.NotFound, $"No se encontro la compra {id}");

        if (compra.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de una compra anulada");

        compra.FechaCompra = fecha;

        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, "Fecha actualizada correctamente");
    }

    public async Task<(ServiceStatus, CompraDto?, string)> ActualizarCompra(int id, CreateCompraPayload payload)
    {
        if (payload.Detalle == null || payload.Detalle.Count == 0)
            return (ServiceStatus.FailedValidation, null, "La compra debe incluir al menos un producto");

        var compra = await _context.Compra.AsTracking()
                                    .Include(c => c.CompraDetalles)
                                    .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            return (ServiceStatus.NotFound, null, $"No se encontro la compra {id}");

        if (compra.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, null, "No se puede editar una compra anulada");

        await _context.Database.BeginTransactionAsync();

        try
        {
            // Revertir el stock de los productos de la compra tal como estaba registrada.
            foreach (var item in compra.CompraDetalles)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null) continue;

                var stockAnterior = producto.Stock ?? 0;
                var stockNuevo = stockAnterior - item.Cantidad;

                if (stockNuevo < 0)
                    return (ServiceStatus.FailedValidation, null, $"Stock insuficiente para editar la compra: el producto {producto.Nombre} ya no tiene suficiente stock para revertir la cantidad original");

                producto.Stock = stockNuevo;

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = stockNuevo,
                    ReferenciaTipo = "CompraEditada",
                    ReferenciaId = compra.Id
                });
            }

            _context.CompraDetalle.RemoveRange(compra.CompraDetalles);

            var nuevoDetalle = payload.Detalle.Select(d => new CompraDetalle
            {
                CompraId = compra.Id,
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                CostoUnitario = d.CostoUnitario
            }).ToList();

            compra.ProveedorId = payload.ProveedorId;
            compra.MetodoPagoId = payload.MetodoPagoId;
            compra.Observacion = payload.Observacion;
            compra.FechaCompra = payload.FechaCompra ?? compra.FechaCompra;
            compra.Total = nuevoDetalle.Sum(d => d.Cantidad * d.CostoUnitario);

            await _context.CompraDetalle.AddRangeAsync(nuevoDetalle);
            await _context.SaveChangesAsync();

            // Aplicar el stock de los productos con la nueva composición de la compra.
            foreach (var item in nuevoDetalle)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null)
                    return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                var stockAnterior = producto.Stock ?? 0;
                producto.Stock = stockAnterior + item.Cantidad;
                producto.CostoUnitario = item.CostoUnitario;

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = producto.Stock.Value,
                    ReferenciaTipo = "CompraEditada",
                    ReferenciaId = compra.Id
                });
            }

            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();

            var (_, dto, _) = await ObtenerCompra(compra.Id);

            return (ServiceStatus.Ok, dto, "Compra actualizada correctamente");
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al editar compra -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}