namespace Domain.Entities;

// Empaque/unidad de venta alternativa para un producto (ej. "Caja x100" vendida
// aparte de la unidad suelta) -- Factor indica cuantas unidades base trae cada presentacion.
public class Presentacion : EntityBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Codigo { get; set; }
    public int UnidadMedidaId { get; set; }
    public UnidadMedida? UnidadMedida { get; set; }
    public decimal Factor { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal? PrecioMinimo { get; set; }
}
