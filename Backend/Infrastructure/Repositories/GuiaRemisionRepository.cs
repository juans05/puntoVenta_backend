using Application.Helper;
using Application.Interfaces.IRepository;
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

// Guia de remision: documento INTERNO de traslado generado desde una Entrega del flujo de ventas
// completo. No se envia a SUNAT -- no hay proxy, credenciales, ticket ni estado externo.
public class GuiaRemisionRepository : IGuiaRemisionRepository
{
    private readonly SpaContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public GuiaRemisionRepository(SpaContext context, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    private async Task<string> GenerarNumero()
        => $"GRE-{((await _context.GuiaRemision.IgnoreQueryFilters().CountAsync(g => g.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";

    public async Task<bool> TieneGuiaActiva(int entregaId)
        => await _context.GuiaRemision.AsNoTracking().AnyAsync(g => g.EntregaId == entregaId && g.EstadoGuia == EstadoGuiaRemision.Emitida);

    public async Task<(ServiceStatus, GuiaRemisionDto?, string)> GenerarDesdeEntrega(int entregaId)
    {
        var entrega = await _context.Entrega.AsNoTracking()
            .Include(e => e.PedidoVenta)
            .Include(e => e.Detalles).ThenInclude(d => d.PedidoVentaDetalle)
            .FirstOrDefaultAsync(e => e.Id == entregaId);
        if (entrega == null) return (ServiceStatus.NotFound, null, "Entrega no encontrada");
        if (entrega.EstadoEntrega != "ACTIVA")
            return (ServiceStatus.FailedValidation, null, "La entrega está anulada");
        if (await TieneGuiaActiva(entregaId))
            return (ServiceStatus.FailedValidation, null, "Esta entrega ya tiene una guía de remisión activa");

        var pedido = entrega.PedidoVenta!;

        try
        {
            var guia = new GuiaRemision
            {
                Numero = await GenerarNumero(),
                SucursalId = entrega.SucursalId,
                EntregaId = entrega.Id,
                FechaEmision = NowLocal(),
                FechaTraslado = entrega.Fecha,
                ClienteNombre = pedido.RazonSocial,
                ClienteDocumento = pedido.NumeroDocumento,
                DireccionLlegada = entrega.Direccion ?? pedido.DireccionCliente,
                Placa = entrega.Placa,
                Detalles = entrega.Detalles.Select(d => new GuiaRemisionDetalle
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    Unidad = null
                }).ToList()
            };

            _context.GuiaRemision.Add(guia);
            await _context.SaveChangesAsync();
            return await Obtener(guia.Id);
        }
        catch (Exception e)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al generar guía de remisión -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    private static readonly string[] ModalidadesValidas = { Domain.Entities.ModalidadTraslado.Publico, Domain.Entities.ModalidadTraslado.Privado };

    public async Task<(ServiceStatus, GuiaRemisionDto?, string)> Actualizar(int id, ActualizarGuiaRemisionPayload payload)
    {
        if (payload.ModTraslado != null && !ModalidadesValidas.Contains(payload.ModTraslado))
            return (ServiceStatus.FailedValidation, null, "Modalidad de traslado no válida");
        if (payload.PesoTotal is < 0)
            return (ServiceStatus.FailedValidation, null, "El peso no puede ser negativo");

        var guia = await _context.GuiaRemision.AsTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (guia == null) return (ServiceStatus.NotFound, null, "Guía no encontrada");
        if (guia.EstadoGuia != EstadoGuiaRemision.Emitida)
            return (ServiceStatus.FailedValidation, null, "La guía está anulada");

        if (payload.FechaTraslado.HasValue) guia.FechaTraslado = payload.FechaTraslado.Value;
        if (payload.Motivo != null) guia.Motivo = payload.Motivo;
        if (payload.ModTraslado != null) guia.ModTraslado = payload.ModTraslado;
        guia.PesoTotal = payload.PesoTotal ?? guia.PesoTotal;
        guia.UndPesoTotal = payload.UndPesoTotal ?? guia.UndPesoTotal;
        guia.UbigeoPartida = payload.UbigeoPartida ?? guia.UbigeoPartida;
        guia.DireccionPartida = payload.DireccionPartida ?? guia.DireccionPartida;
        guia.UbigeoLlegada = payload.UbigeoLlegada ?? guia.UbigeoLlegada;
        guia.DireccionLlegada = payload.DireccionLlegada ?? guia.DireccionLlegada;
        guia.TransportistaRuc = payload.TransportistaRuc ?? guia.TransportistaRuc;
        guia.TransportistaRazonSocial = payload.TransportistaRazonSocial ?? guia.TransportistaRazonSocial;
        guia.TransportistaMtc = payload.TransportistaMtc ?? guia.TransportistaMtc;
        guia.ChoferNombre = payload.ChoferNombre ?? guia.ChoferNombre;
        guia.ChoferDocumento = payload.ChoferDocumento ?? guia.ChoferDocumento;
        guia.Placa = payload.Placa ?? guia.Placa;

        await _context.SaveChangesAsync();
        return await Obtener(id);
    }

    private IQueryable<GuiaRemision> Query() => _context.GuiaRemision.AsNoTracking()
        .Include(g => g.Entrega).ThenInclude(e => e!.PedidoVenta)
        .Include(g => g.Detalles).ThenInclude(d => d.Producto);

    private static GuiaRemisionDto ToDto(GuiaRemision g) => new()
    {
        Id = g.Id,
        Numero = g.Numero,
        EntregaId = g.EntregaId,
        EntregaNumero = g.Entrega?.Numero,
        PedidoVentaId = g.Entrega?.PedidoVentaId ?? 0,
        PedidoVentaNumero = g.Entrega?.PedidoVenta?.Numero,
        FechaEmision = g.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
        FechaTraslado = g.FechaTraslado.ToString("dd/MM/yyyy"),
        ClienteNombre = g.ClienteNombre,
        ClienteDocumento = g.ClienteDocumento,
        Motivo = g.Motivo,
        ModTraslado = g.ModTraslado,
        PesoTotal = g.PesoTotal,
        UndPesoTotal = g.UndPesoTotal,
        UbigeoPartida = g.UbigeoPartida,
        DireccionPartida = g.DireccionPartida,
        UbigeoLlegada = g.UbigeoLlegada,
        DireccionLlegada = g.DireccionLlegada,
        TransportistaRuc = g.TransportistaRuc,
        TransportistaRazonSocial = g.TransportistaRazonSocial,
        TransportistaMtc = g.TransportistaMtc,
        ChoferNombre = g.ChoferNombre,
        ChoferDocumento = g.ChoferDocumento,
        Placa = g.Placa,
        EstadoGuia = g.EstadoGuia,
        Detalle = g.Detalles.Select(d => new GuiaRemisionDetalleDto
        {
            ProductoId = d.ProductoId,
            Producto = d.Producto?.Nombre,
            Cantidad = d.Cantidad,
            Unidad = d.Unidad
        }).ToList()
    };

    public async Task<(ServiceStatus, DataCollection<GuiaRemisionDto>?, string)> Listar(GuiaRemisionQueryParams payload)
    {
        try
        {
            var query = Query().Where(g => g.Estado);
            var total = await query.CountAsync();
            var page = Math.Max(1, payload.Page);
            var amount = Math.Max(1, payload.Amount);
            var items = await query.OrderByDescending(g => g.Id).Skip((page - 1) * amount).Take(amount).ToListAsync();

            return (ServiceStatus.Ok, new DataCollection<GuiaRemisionDto>
            {
                Items = items.Select(ToDto).ToList(), Total = total, Page = page, Pages = (int)Math.Ceiling(total / (double)amount)
            }, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar guías -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, GuiaRemisionDto?, string)> Obtener(int id)
    {
        var guia = await Query().FirstOrDefaultAsync(g => g.Id == id);
        return guia == null ? (ServiceStatus.NotFound, null, "Guía no encontrada") : (ServiceStatus.Ok, ToDto(guia), "Success");
    }

    public async Task<(ServiceStatus, GuiaRemisionDto?, string)> Anular(int id)
    {
        var guia = await _context.GuiaRemision.AsTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (guia == null) return (ServiceStatus.NotFound, null, "Guía no encontrada");
        if (guia.EstadoGuia != EstadoGuiaRemision.Emitida)
            return (ServiceStatus.FailedValidation, null, "La guía ya está anulada");

        guia.EstadoGuia = EstadoGuiaRemision.Anulada;
        await _context.SaveChangesAsync();
        return await Obtener(id);
    }
}
