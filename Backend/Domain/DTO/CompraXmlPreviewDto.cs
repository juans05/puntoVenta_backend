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
    // Codigo SUNAT de la moneda del XML (PEN, USD...).
    public string? Moneda { get; set; }
    public List<CompraXmlLineaDto> Lineas { get; set; } = new();
}

public class CompraXmlLineaDto
{
    public string Descripcion { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    // Codigo del item segun el proveedor (cac:SellersItemIdentification), si lo trae.
    public string? Codigo { get; set; }
    // Afectacion IGV de la linea segun SUNAT (tabla 07: 10 gravado, 20 exonerado, 30 inafecto...).
    public string? AfectacionIgv { get; set; }
    // Producto del catalogo ya asociado a esta linea por una compra anterior (ProductoProveedor).
    public int? ProductoId { get; set; }
    public string? Producto { get; set; }
}

// Resultado de "Descargar y registrar" desde SUNAT: registrada (con su compra) o por revisar (con el motivo
// y la lectura del XML para abrirla en el formulario).
public class ResultadoXmlSunatDto
{
    public bool Registrada { get; set; }
    public int? CompraId { get; set; }
    public string? Motivo { get; set; }
    public CompraXmlPreviewDto? Preview { get; set; }
}
