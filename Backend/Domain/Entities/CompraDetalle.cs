namespace Domain.Entities;

public class CompraDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}