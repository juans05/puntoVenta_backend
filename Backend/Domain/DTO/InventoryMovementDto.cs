namespace Domain.DTO;

public class InventoryMovementDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? Producto { get; set; }
    public string TipoMovimiento { get; set; } = null!;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockPosterior { get; set; }
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    public string Fecha { get; set; } = null!;
    public string? Usuario { get; set; }
}