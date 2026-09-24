using Application.Helper;
using Application.Interfaces.IRepository;
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

// Flujo de ventas completo: Pedido de venta (reserva stock) -> Entrega (baja stock) -> Factura desde lo
// entregado (ver PedidoVentaFacturacion). Solo aplica con ConfiguracionFlujo.FlujoVentas = COMPLETO;
// la venta de mostrador y el modo simplificado no pasan por aqui.
public class PedidoVentaRepository : IPedidoVentaRepository
{
    private readonly SpaContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IGuiaRemisionRepository _guiaRemisionRepository;

    public PedidoVentaRepository(SpaContext context, IHttpContextAccessor? httpContextAccessor, IGuiaRemisionRepository guiaRemisionRepository)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _guiaRemisionRepository = guiaRemisionRepository;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    private async Task<string?> ValidarFlujoCompleto()
    {
        var config = await _context.ConfiguracionFlujo.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();
        return (config?.FlujoVentas ?? FlujoComprasModo.Simplificado) == FlujoComprasModo.Completo
            ? null
            : "El flujo completo de ventas no está activo (Configuración → Flujo)";
    }

    private async Task<string> GenerarNumero()
        => $"PV-{((await _context.PedidoVenta.IgnoreQueryFilters().CountAsync(o => o.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";

    private async Task<string> GenerarNumeroEntrega()
        => $"ENT-{((await _context.Entrega.IgnoreQueryFilters().CountAsync(o => o.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";

    private static string? ValidarDetalle(List<PedidoVentaDetallePayload>? detalle)
    {
        if (detalle == null || detalle.Count == 0) return "El pedido debe incluir al menos un producto";
        if (detalle.Any(d => d.Cantidad <= 0)) return "Las cantidades deben ser mayores a cero";
        if (detalle.Any(d => d.ValorUnitario < 0)) return "El precio no puede ser negativo";
        if (detalle.GroupBy(d => d.ProductoId).Any(g => g.Count() > 1)) return "Un producto no puede repetirse en el mismo pedido";
        return null;
    }

    // Stock libre = stock - lo que otros pedidos reservan (pendiente de entregar).
    private async Task<string?> ValidarDisponibilidad(PedidoVenta pedido)
    {
        foreach (var d in pedido.Detalles)
        {
            var producto = await _context.Producto.AsNoTracking().FirstOrDefaultAsync(p => p.Id == d.ProductoId);
            if (producto == null) return $"No se encontró el producto {d.ProductoId}";

            var reservado = await _context.PedidoVentaDetalle
                .Where(x => x.ProductoId == d.ProductoId && x.PedidoVentaId != pedido.Id
                         && EstadosPedidoVenta.Reservan.Contains(x.PedidoVenta!.EstadoPedidoVenta))
                .SumAsync(x => (int?)(x.CantidadPedida - x.CantidadEntregada)) ?? 0;

            var disponible = (producto.Stock ?? 0) - reservado;
            if (disponible < d.CantidadPedida)
                return $"Stock insuficiente de {producto.Nombre}: disponible {Math.Max(0, disponible)}, pedido {d.CantidadPedida}";
        }
        return null;
    }

    private static List<PedidoVentaDetalle> ADetalles(IEnumerable<PedidoVentaDetallePayload> lineas) => lineas.Select(d => new PedidoVentaDetalle
    {
        ProductoId = d.ProductoId,
        CantidadPedida = d.Cantidad,
        ValorUnitario = d.ValorUnitario,
        TipoIgvId = d.TipoIgvId,
        UnidadMedidaId = d.UnidadMedidaId
    }).ToList();

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> Crear(CreatePedidoVentaPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (ValidarDetalle(payload.Detalle) is { } error) return (ServiceStatus.FailedValidation, null, error);

        try
        {
            var pedido = new PedidoVenta
            {
                Numero = await GenerarNumero(),
                SucursalId = payload.SucursalId,
                ClienteId = payload.ClienteId,
                NumeroDocumento = payload.NumeroDocumento,
                RazonSocial = payload.RazonSocial,
                DireccionCliente = payload.DireccionCliente,
                FechaEmision = NowLocal(),
                Observacion = payload.Observacion,
                Total = payload.Detalle.Sum(d => d.Cantidad * d.ValorUnitario),
                EstadoPedidoVenta = EstadosPedidoVenta.Borrador,
                Detalles = ADetalles(payload.Detalle)
            };

            _context.PedidoVenta.Add(pedido);
            await _context.SaveChangesAsync();

            return payload.Borrador ? await Obtener(pedido.Id) : await Confirmar(pedido.Id);
        }
        catch (Exception e)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al crear pedido -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    // Convierte una cotizacion vigente en pedido de venta (BORRADOR) con sus lineas.
    public async Task<(ServiceStatus, PedidoVentaDto?, string)> CrearDesdeCotizacion(int cotizacionId)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);

        var cotizacion = await _context.ComprobanteCabecera.AsNoTracking()
            .Include(c => c.ComprobanteDetalles)
            .FirstOrDefaultAsync(c => c.Id == cotizacionId && c.TipoDocumentoVentaId == (int)TipoComprobante.Cotizacion);
        if (cotizacion == null) return (ServiceStatus.NotFound, null, "No se encontró la cotización");
        if (cotizacion.EstadoComprobante == EstatusComprobante.Anulado)
            return (ServiceStatus.FailedValidation, null, "La cotización se encuentra anulada");
        if (cotizacion.FechaVigencia.HasValue && cotizacion.FechaVigencia.Value.Date < DateTime.UtcNow.AddHours(-5).Date)
            return (ServiceStatus.FailedValidation, null, "La cotización está vencida");
        if (await _context.PedidoVenta.AnyAsync(p => p.CotizacionOrigenId == cotizacionId && p.EstadoPedidoVenta != EstadosPedidoVenta.Anulado))
            return (ServiceStatus.FailedValidation, null, "Esta cotización ya fue convertida en pedido de venta");

        var lineas = cotizacion.ComprobanteDetalles
            .GroupBy(d => d.ProductoId)
            .Select(g => new PedidoVentaDetallePayload
            {
                ProductoId = g.Key,
                Cantidad = g.Sum(x => x.Cantidad),
                ValorUnitario = g.First().ValorUnitario,
                TipoIgvId = g.First().TipoIgvId,
                UnidadMedidaId = g.First().UnidadMedidaId
            }).ToList();

        var pedido = new PedidoVenta
        {
            Numero = await GenerarNumero(),
            SucursalId = cotizacion.SucursalId,
            ClienteId = cotizacion.ClienteId,
            NumeroDocumento = cotizacion.NumeroDocumento,
            RazonSocial = cotizacion.RazonSocial,
            FechaEmision = NowLocal(),
            CotizacionOrigenId = cotizacionId,
            Total = lineas.Sum(d => d.Cantidad * d.ValorUnitario),
            EstadoPedidoVenta = EstadosPedidoVenta.Borrador,
            Detalles = ADetalles(lineas)
        };
        _context.PedidoVenta.Add(pedido);
        await _context.SaveChangesAsync();
        return await Obtener(pedido.Id);
    }

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> Actualizar(int id, CreatePedidoVentaPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (ValidarDetalle(payload.Detalle) is { } error) return (ServiceStatus.FailedValidation, null, error);

        var pedido = await CargarTracking(id);
        if (pedido == null) return (ServiceStatus.NotFound, null, "Pedido no encontrado");
        if (pedido.EstadoPedidoVenta != EstadosPedidoVenta.Borrador)
            return (ServiceStatus.FailedValidation, null, "Solo se puede editar un pedido en borrador");

        _context.PedidoVentaDetalle.RemoveRange(pedido.Detalles);
        pedido.Detalles = ADetalles(payload.Detalle);
        pedido.SucursalId = payload.SucursalId;
        pedido.ClienteId = payload.ClienteId;
        pedido.NumeroDocumento = payload.NumeroDocumento;
        pedido.RazonSocial = payload.RazonSocial;
        pedido.DireccionCliente = payload.DireccionCliente;
        pedido.Observacion = payload.Observacion;
        pedido.Total = payload.Detalle.Sum(d => d.Cantidad * d.ValorUnitario);
        await _context.SaveChangesAsync();

        return payload.Borrador ? await Obtener(id) : await Confirmar(id);
    }

    private IQueryable<PedidoVenta> Query() => _context.PedidoVenta.AsNoTracking()
        .Include(o => o.Sucursal)
        .Include(o => o.Detalles).ThenInclude(d => d.Producto)
        .Include(o => o.Entregas);

    private static PedidoVentaDto ToDto(PedidoVenta o) => new()
    {
        Id = o.Id,
        Numero = o.Numero,
        SucursalId = o.SucursalId,
        Sucursal = o.Sucursal?.Nombre,
        ClienteId = o.ClienteId,
        NumeroDocumento = o.NumeroDocumento,
        RazonSocial = o.RazonSocial,
        DireccionCliente = o.DireccionCliente,
        FechaEmision = o.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
        Total = o.Total,
        EstadoPedidoVenta = o.EstadoPedidoVenta,
        Observacion = o.Observacion,
        CotizacionOrigenId = o.CotizacionOrigenId,
        MotivoCierre = o.MotivoCierre,
        Usuario = o.UsuarioCreacion,
        Detalle = o.Detalles.Select(d => new PedidoVentaDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            Producto = d.Producto?.Nombre,
            CantidadPedida = d.CantidadPedida,
            CantidadEntregada = d.CantidadEntregada,
            CantidadFacturada = d.CantidadFacturada,
            ValorUnitario = d.ValorUnitario,
            TipoIgvId = d.TipoIgvId,
            UnidadMedidaId = d.UnidadMedidaId
        }).ToList(),
        Entregas = o.Entregas.OrderBy(e => e.Id).Select(e => new EntregaDto
        {
            Id = e.Id,
            Numero = e.Numero,
            Fecha = e.Fecha.ToString("dd/MM/yyyy HH:mm"),
            EstadoEntrega = e.EstadoEntrega,
            Placa = e.Placa,
            Direccion = e.Direccion,
            Observacion = e.Observacion
        }).ToList()
    };

    public async Task<(ServiceStatus, DataCollection<PedidoVentaDto>?, string)> Listar(PedidoVentaQueryParams payload)
    {
        try
        {
            var query = Query().Where(o => o.Estado);
            if (!string.IsNullOrWhiteSpace(payload.Estado))
                query = query.Where(o => o.EstadoPedidoVenta == payload.Estado);

            var total = await query.CountAsync();
            var page = Math.Max(1, payload.Page);
            var amount = Math.Max(1, payload.Amount);
            var items = await query.OrderByDescending(o => o.Id).Skip((page - 1) * amount).Take(amount).ToListAsync();

            return (ServiceStatus.Ok, new DataCollection<PedidoVentaDto>
            {
                Items = items.Select(ToDto).ToList(),
                Total = total,
                Page = page,
                Pages = (int)Math.Ceiling(total / (double)amount)
            }, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar pedidos -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> Obtener(int id)
    {
        var pedido = await Query().FirstOrDefaultAsync(o => o.Id == id);
        return pedido == null
            ? (ServiceStatus.NotFound, null, "Pedido no encontrado")
            : (ServiceStatus.Ok, ToDto(pedido), "Success");
    }

    private Task<PedidoVenta?> CargarTracking(int id)
        => _context.PedidoVenta.AsTracking().Include(o => o.Detalles).Include(o => o.Entregas).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> Confirmar(int id)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var pedido = await CargarTracking(id);
        if (pedido == null) return (ServiceStatus.NotFound, null, "Pedido no encontrado");
        if (pedido.EstadoPedidoVenta != EstadosPedidoVenta.Borrador)
            return (ServiceStatus.FailedValidation, null, "Solo se confirma un pedido en borrador");
        if (await ValidarDisponibilidad(pedido) is { } sinStock)
            return (ServiceStatus.FailedValidation, null, sinStock);

        pedido.EstadoPedidoVenta = EstadosPedidoVenta.Confirmado;
        await _context.SaveChangesAsync();
        return await Obtener(id);
    }

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> Anular(int id)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var pedido = await CargarTracking(id);
        if (pedido == null) return (ServiceStatus.NotFound, null, "Pedido no encontrado");
        if (pedido.EstadoPedidoVenta is EstadosPedidoVenta.Anulado or EstadosPedidoVenta.Cerrado)
            return (ServiceStatus.FailedValidation, null, "El pedido ya está anulado o cerrado");
        if (pedido.Entregas.Any(e => e.EstadoEntrega == "ACTIVA"))
            return (ServiceStatus.FailedValidation, null, "Anula primero las entregas del pedido");
        if (pedido.Detalles.Any(d => d.CantidadFacturada > 0))
            return (ServiceStatus.FailedValidation, null, "El pedido tiene comprobantes emitidos: anúlalos primero");

        pedido.EstadoPedidoVenta = EstadosPedidoVenta.Anulado;
        await _context.SaveChangesAsync();
        return await Obtener(id);
    }

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> Cerrar(int id, string? motivo)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (string.IsNullOrWhiteSpace(motivo))
            return (ServiceStatus.FailedValidation, null, "Indica el motivo del cierre");
        var pedido = await CargarTracking(id);
        if (pedido == null) return (ServiceStatus.NotFound, null, "Pedido no encontrado");
        if (pedido.EstadoPedidoVenta is not (EstadosPedidoVenta.Confirmado or EstadosPedidoVenta.EntregadoParcial or EstadosPedidoVenta.Entregado))
            return (ServiceStatus.FailedValidation, null, "El pedido no se puede cerrar en su estado actual");

        pedido.MotivoCierre = motivo.Trim();
        pedido.EstadoPedidoVenta = EstadosPedidoVenta.Cerrado;
        await _context.SaveChangesAsync();
        return await Obtener(id);
    }

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> RegistrarEntrega(int pedidoId, CreateEntregaPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (payload.Detalle == null || payload.Detalle.Any(d => d.Cantidad < 0))
            return (ServiceStatus.FailedValidation, null, "Las cantidades no pueden ser negativas");
        var lineas = payload.Detalle.Where(d => d.Cantidad > 0).ToList();
        if (lineas.Count == 0) return (ServiceStatus.FailedValidation, null, "Indica al menos una cantidad a entregar");

        var pedido = await CargarTracking(pedidoId);
        if (pedido == null) return (ServiceStatus.NotFound, null, "Pedido no encontrado");
        if (pedido.EstadoPedidoVenta is not (EstadosPedidoVenta.Confirmado or EstadosPedidoVenta.EntregadoParcial))
            return (ServiceStatus.FailedValidation, null, "Solo se entrega un pedido confirmado o con entrega parcial");

        foreach (var l in lineas)
        {
            var pd = pedido.Detalles.FirstOrDefault(d => d.Id == l.PedidoVentaDetalleId);
            if (pd == null) return (ServiceStatus.FailedValidation, null, "Una línea no pertenece al pedido");
            if (l.Cantidad > pd.CantidadPedida - pd.CantidadEntregada)
                return (ServiceStatus.FailedValidation, null, $"No puedes entregar más de lo pendiente ({pd.CantidadPedida - pd.CantidadEntregada}) de la línea {pd.Id}");
        }

        await _context.Database.BeginTransactionAsync();
        try
        {
            var entrega = new Entrega
            {
                Numero = await GenerarNumeroEntrega(),
                SucursalId = pedido.SucursalId,
                PedidoVentaId = pedido.Id,
                Fecha = NowLocal(),
                Placa = payload.Placa,
                Direccion = payload.Direccion ?? pedido.DireccionCliente,
                Observacion = payload.Observacion
            };
            _context.Entrega.Add(entrega);
            await _context.SaveChangesAsync();

            foreach (var l in lineas)
            {
                var pd = pedido.Detalles.First(d => d.Id == l.PedidoVentaDetalleId);
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == pd.ProductoId);
                if (producto == null)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, $"No se encontró el producto {pd.ProductoId}");
                }

                var stockAnterior = producto.Stock ?? 0;
                if (stockAnterior < l.Cantidad)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, $"No hay stock disponible para {producto.Nombre}");
                }

                pd.CantidadEntregada += l.Cantidad;
                producto.Stock = stockAnterior - l.Cantidad;

                _context.EntregaDetalle.Add(new EntregaDetalle
                {
                    EntregaId = entrega.Id,
                    PedidoVentaDetalleId = pd.Id,
                    ProductoId = pd.ProductoId,
                    Cantidad = l.Cantidad
                });
                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Venta,
                    Cantidad = l.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = producto.Stock.Value,
                    ReferenciaTipo = "Entrega",
                    ReferenciaId = entrega.Id
                });
            }

            EstadosPedidoVenta.Recalcular(pedido);
            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();
            return await Obtener(pedidoId);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al registrar entrega -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, PedidoVentaDto?, string)> AnularEntrega(int entregaId)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var entrega = await _context.Entrega.AsTracking().Include(e => e.Detalles).FirstOrDefaultAsync(e => e.Id == entregaId);
        if (entrega == null) return (ServiceStatus.NotFound, null, "Entrega no encontrada");
        if (entrega.EstadoEntrega != "ACTIVA")
            return (ServiceStatus.FailedValidation, null, "La entrega ya está anulada");
        if (await _guiaRemisionRepository.TieneGuiaActiva(entregaId))
            return (ServiceStatus.FailedValidation, null, "Esta entrega tiene una guía de remisión activa: anúlala primero");

        var pedido = await CargarTracking(entrega.PedidoVentaId);
        if (pedido == null) return (ServiceStatus.NotFound, null, "Pedido no encontrado");
        if (pedido.EstadoPedidoVenta == EstadosPedidoVenta.Anulado)
            return (ServiceStatus.FailedValidation, null, "El pedido está anulado");

        foreach (var d in entrega.Detalles)
        {
            var pd = pedido.Detalles.First(x => x.Id == d.PedidoVentaDetalleId);
            if (pd.CantidadEntregada - d.Cantidad < pd.CantidadFacturada)
                return (ServiceStatus.FailedValidation, null, "Hay comprobantes emitidos sobre esta entrega: anúlalos primero");
        }

        await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var d in entrega.Detalles)
            {
                var pd = pedido.Detalles.First(x => x.Id == d.PedidoVentaDetalleId);
                pd.CantidadEntregada -= d.Cantidad;

                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == d.ProductoId);
                if (producto == null) continue;

                var stockAnterior = producto.Stock ?? 0;
                producto.Stock = stockAnterior + d.Cantidad;
                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionVenta,
                    Cantidad = d.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = producto.Stock.Value,
                    ReferenciaTipo = "EntregaAnulada",
                    ReferenciaId = entrega.Id
                });
            }

            entrega.EstadoEntrega = "ANULADA";
            pedido.MotivoCierre = null;
            if (pedido.EstadoPedidoVenta == EstadosPedidoVenta.Cerrado) pedido.EstadoPedidoVenta = EstadosPedidoVenta.Entregado;
            EstadosPedidoVenta.Recalcular(pedido);
            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();
            return await Obtener(pedido.Id);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al anular entrega -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}
