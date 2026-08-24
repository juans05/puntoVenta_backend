namespace Domain.Entities;

public class InventoryMovement : EntityBase
{
    public int? SucursalId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int TipoMovimiento { get; set; }
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockPosterior { get; set; }

    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
}