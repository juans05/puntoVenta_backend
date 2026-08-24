namespace Domain.Entities;

public class Compra : EntityBase
{
    public int? SucursalId { get; set; }
    public string NumeroCompra { get; set; } = null!;
    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public decimal Total { get; set; }
    public int? MetodoPagoId { get; set; }
    public Metodopago? Metodopago { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public DateTime FechaCompra { get; set; }
    public string? Observacion { get; set; }
    public List<CompraDetalle> CompraDetalles { get; set; } = new();
}