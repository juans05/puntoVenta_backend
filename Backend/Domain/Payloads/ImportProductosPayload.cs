namespace Domain.Payloads;

public class ImportProductosPayload
{
    public string Csv { get; set; } = null!;
}

public class ProductoCsvRow
{
    public string? Sku { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Categoria { get; set; }
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Stock { get; set; }
    public int? StockMinimo { get; set; }
    public string? Error { get; set; }
}