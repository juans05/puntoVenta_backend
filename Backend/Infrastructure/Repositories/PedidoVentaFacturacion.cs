using Domain.Entities;
using Domain.Enumerations;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Puente entre la emision de comprobantes y el pedido de venta (flujo de ventas COMPLETO):
// valida que lo que se factura este entregado y aun sin facturar, y mantiene CantidadFacturada.
public static class PedidoVentaFacturacion
{
    // Devuelve el motivo del rechazo, o null si el comprobante puede emitirse contra el pedido.
    public static async Task<string?> Validar(SpaContext context, ComprobantePayload payload)
    {
        var config = await context.ConfiguracionFlujo.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();
        if ((config?.FlujoVentas ?? FlujoComprasModo.Simplificado) != FlujoComprasModo.Completo)
            return "El flujo completo de ventas no está activo (Configuración → Flujo)";

        if (payload.TipoDocumentoVentaId is not ((int)TipoComprobante.Factura or (int)TipoComprobante.Boleta or (int)TipoComprobante.TicketInterno))
            return "Solo se factura un pedido de venta con factura, boleta o nota de venta";

        var pedido = await context.PedidoVenta.AsNoTracking().Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == payload.PedidoVentaId);
        if (pedido == null)
            return "No se encontró el pedido de venta";
        if (pedido.EstadoPedidoVenta is not (EstadosPedidoVenta.EntregadoParcial or EstadosPedidoVenta.Entregado))
            return "El pedido no tiene mercadería entregada pendiente de facturar";

        foreach (var linea in payload.DetalleComprobante.GroupBy(l => l.ProductoId))
        {
            var od = pedido.Detalles.FirstOrDefault(d => d.ProductoId == linea.Key);
            if (od == null)
                return $"El producto {linea.Key} no pertenece al pedido";
            var pendiente = od.CantidadEntregada - od.CantidadFacturada;
            if (linea.Sum(l => l.Cantidad) > pendiente)
                return $"Solo hay {pendiente} unidad(es) entregadas por facturar del producto {linea.Key}";
        }

        return null;
    }

    // signo +1 al emitir, -1 al anular. El llamador hace SaveChanges (misma transaccion del comprobante).
    public static async Task Aplicar(SpaContext context, int pedidoVentaId, IEnumerable<(int ProductoId, int Cantidad)> lineas, int signo)
    {
        var pedido = await context.PedidoVenta.AsTracking().Include(p => p.Detalles).FirstAsync(p => p.Id == pedidoVentaId);
        foreach (var (productoId, cantidad) in lineas)
        {
            var od = pedido.Detalles.FirstOrDefault(d => d.ProductoId == productoId);
            if (od != null) od.CantidadFacturada = Math.Max(0, od.CantidadFacturada + signo * cantidad);
        }
        // Al anular una factura de un pedido cerrado a mano (MotivoCierre) se reabre para recalcular.
        if (signo < 0)
        {
            pedido.MotivoCierre = null;
            if (pedido.EstadoPedidoVenta == EstadosPedidoVenta.Cerrado) pedido.EstadoPedidoVenta = EstadosPedidoVenta.Entregado;
        }
        EstadosPedidoVenta.Recalcular(pedido);
    }
}
