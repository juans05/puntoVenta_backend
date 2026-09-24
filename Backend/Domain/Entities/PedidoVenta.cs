namespace Domain.Entities;

// Pedido de venta interno del flujo completo de ventas (no confundir con Pedido = pedido online).
public class PedidoVenta : EntityBase
{
    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }
    public string Numero { get; set; } = null!;
    public int? ClienteId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? DireccionCliente { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal Total { get; set; }
    public string EstadoPedidoVenta { get; set; } = EstadosPedidoVenta.Borrador;
    public string? Observacion { get; set; }
    public int? CotizacionOrigenId { get; set; }
    public string? MotivoCierre { get; set; }
    public List<PedidoVentaDetalle> Detalles { get; set; } = new();
    public List<Entrega> Entregas { get; set; } = new();
}

public class PedidoVentaDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int PedidoVentaId { get; set; }
    public PedidoVenta? PedidoVenta { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int CantidadPedida { get; set; }
    public int CantidadEntregada { get; set; }
    public int CantidadFacturada { get; set; }
    public decimal ValorUnitario { get; set; }
    public int? TipoIgvId { get; set; }
    public int? UnidadMedidaId { get; set; }
}

public class Entrega : EntityBase
{
    public int? SucursalId { get; set; }
    public string Numero { get; set; } = null!;
    public int PedidoVentaId { get; set; }
    public PedidoVenta? PedidoVenta { get; set; }
    public DateTime Fecha { get; set; }
    public string EstadoEntrega { get; set; } = "ACTIVA";
    public string? Placa { get; set; }
    public string? Direccion { get; set; }
    public string? Observacion { get; set; }
    public List<EntregaDetalle> Detalles { get; set; } = new();
}

public class EntregaDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int EntregaId { get; set; }
    public Entrega? Entrega { get; set; }
    public int PedidoVentaDetalleId { get; set; }
    public PedidoVentaDetalle? PedidoVentaDetalle { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}

public static class EstadosPedidoVenta
{
    public const string Borrador = "BORRADOR";
    public const string Confirmado = "CONFIRMADO";
    public const string EntregadoParcial = "ENTREGADO_PARCIAL";
    public const string Entregado = "ENTREGADO";
    public const string Cerrado = "CERRADO";
    public const string Anulado = "ANULADO";

    // Estados que reservan stock (lo pendiente de entregar).
    public static readonly string[] Reservan = { Confirmado, EntregadoParcial };

    // Estado derivado de las cantidades. No toca borrador/anulado ni los cerrados a mano.
    public static void Recalcular(PedidoVenta pedido)
    {
        if (pedido.EstadoPedidoVenta is Borrador or Anulado) return;
        if (pedido.MotivoCierre != null)
        {
            pedido.EstadoPedidoVenta = Cerrado;
            return;
        }

        var entregado = pedido.Detalles.Sum(d => d.CantidadEntregada);
        var todoEntregado = pedido.Detalles.All(d => d.CantidadEntregada >= d.CantidadPedida);
        var todoFacturado = pedido.Detalles.All(d => d.CantidadFacturada >= d.CantidadEntregada);

        pedido.EstadoPedidoVenta = entregado == 0 ? Confirmado
            : !todoEntregado ? EntregadoParcial
            : todoFacturado ? Cerrado
            : Entregado;
    }
}
