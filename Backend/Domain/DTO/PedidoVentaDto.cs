namespace Domain.DTO;

public class PedidoVentaDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public int? SucursalId { get; set; }
    public string? Sucursal { get; set; }
    public int? ClienteId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? DireccionCliente { get; set; }
    public string FechaEmision { get; set; } = null!;
    public decimal Total { get; set; }
    public string EstadoPedidoVenta { get; set; } = null!;
    public string? Observacion { get; set; }
    public int? CotizacionOrigenId { get; set; }
    public string? MotivoCierre { get; set; }
    public string? Usuario { get; set; }
    public List<PedidoVentaDetalleDto> Detalle { get; set; } = new();
    public List<EntregaDto> Entregas { get; set; } = new();
}

public class PedidoVentaDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? Producto { get; set; }
    public int CantidadPedida { get; set; }
    public int CantidadEntregada { get; set; }
    public int CantidadFacturada { get; set; }
    public decimal ValorUnitario { get; set; }
    public int? TipoIgvId { get; set; }
    public int? UnidadMedidaId { get; set; }
}

public class EntregaDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public string Fecha { get; set; } = null!;
    public string EstadoEntrega { get; set; } = null!;
    public string? Placa { get; set; }
    public string? Direccion { get; set; }
    public string? Observacion { get; set; }
}
