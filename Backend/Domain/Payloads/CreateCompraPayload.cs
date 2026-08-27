namespace Domain.Payloads;

public class CreateCompraPayload
{
    public int? ProveedorId { get; set; }
    public int? MetodoPagoId { get; set; }
    public string? Observacion { get; set; }
    public DateTime? FechaCompra { get; set; }
    public List<CompraDetallePayload> Detalle { get; set; } = new();
}

public class CompraDetallePayload
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}