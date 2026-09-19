namespace Domain.Entities;

// Precio adicional que un producto puede tener ademas de su precio estandar
// (ej. Mayorista/VIP/Distribuidor) -- el cajero elige cual cobrar al momento de la venta.
public class PrecioAlternativo : EntityBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PrecioVenta { get; set; }
}
