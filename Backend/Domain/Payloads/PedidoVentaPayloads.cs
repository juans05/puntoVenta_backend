namespace Domain.Payloads;

public class PedidoVentaQueryParams
{
    public int Page { get; set; } = 1;
    public int Amount { get; set; } = 50;
    public string? Estado { get; set; }
}

public class CreatePedidoVentaPayload
{
    public int? SucursalId { get; set; }
    public int? ClienteId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? DireccionCliente { get; set; }
    public string? Observacion { get; set; }
    // true = queda en BORRADOR; false = se confirma (reserva stock).
    public bool Borrador { get; set; }
    public List<PedidoVentaDetallePayload> Detalle { get; set; } = new();
}

public class PedidoVentaDetallePayload
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
    public int? TipoIgvId { get; set; }
    public int? UnidadMedidaId { get; set; }
}

public class CerrarPedidoVentaPayload
{
    public string? Motivo { get; set; }
}

public class CreateEntregaPayload
{
    public string? Placa { get; set; }
    public string? Direccion { get; set; }
    public string? Observacion { get; set; }
    public List<EntregaDetallePayload> Detalle { get; set; } = new();
}

public class EntregaDetallePayload
{
    public int PedidoVentaDetalleId { get; set; }
    public int Cantidad { get; set; }
}
