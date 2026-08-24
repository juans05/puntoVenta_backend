namespace Domain.Payloads;

public class InventoryMovementQuery
{
    public int Page { get; set; } = 1;

    public int Amount { get; set; } = 20;

    public int? ProductoId { get; set; }

    public int? TipoMovimiento { get; set; }

    public string? Fecha { get; set; }
}