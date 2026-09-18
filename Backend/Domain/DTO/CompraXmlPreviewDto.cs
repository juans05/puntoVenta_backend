namespace Domain.DTO;

// Resultado de leer un XML UBL 2.1 de factura de compra (formato estandar SUNAT) -- es solo
// una previsualizacion: no crea la Compra todavia, el usuario revisa/completa los productos
// (el XML no dice a que Producto del catalogo interno corresponde cada linea) y confirma
// desde el mismo formulario de "Nueva compra".
public class CompraXmlPreviewDto
{
    public int? ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorRuc { get; set; }
    public string? NumeroDocumento { get; set; }
    public DateTime? FechaEmision { get; set; }
    public decimal Total { get; set; }
    public List<CompraXmlLineaDto> Lineas { get; set; } = new();
}

public class CompraXmlLineaDto
{
    public string Descripcion { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
