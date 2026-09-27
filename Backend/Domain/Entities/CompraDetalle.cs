namespace Domain.Entities;

public class CompraDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }
    // Linea de bien: ProductoId del catalogo. Linea de servicio: null + Descripcion libre.
    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string? Descripcion { get; set; }
    // Linea de la orden de origen (solo cuando la compra viene de FacturarOrden).
    public int? OrdenCompraDetalleId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}