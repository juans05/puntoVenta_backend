using System.Globalization;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/facturacion")]
[ApiController]
public class FacturacionController : ControllerBase
{
    private readonly IComprobanteService _comprobanteService;
    private readonly IRentaService _rentaService;

    public FacturacionController(IComprobanteService comprobanteService, IRentaService rentaService)
    {
        _comprobanteService = comprobanteService;
        _rentaService = rentaService;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearComprobante(ComprobantePayload payload) => Ok(await _comprobanteService.CrearComprobante(payload));

    // Lee un XML UBL (factura/boleta de SUNAT) y devuelve cliente + lineas para precargar la venta.
    // No guarda nada: el usuario revisa y emite desde el formulario.
    [HttpPost("importar-xml")]
    public IActionResult ImportarXmlVenta(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "Selecciona un archivo XML" });

        XDocument doc;
        try
        {
            using var stream = archivo.OpenReadStream();
            doc = XDocument.Load(stream);
        }
        catch (Exception)
        {
            return BadRequest(new { message = "El archivo no es un XML válido" });
        }

        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        var root = doc.Root;
        var lineasXml = root?.Elements(cac + "InvoiceLine").ToList() ?? new List<XElement>();
        if (root == null || lineasXml.Count == 0)
            return BadRequest(new { message = "No se pudo leer el XML: no tiene la estructura de una factura electrónica UBL de SUNAT" });

        static decimal Num(string? v) =>
            decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;

        var cliente = root.Element(cac + "AccountingCustomerParty")?.Element(cac + "Party");
        var lineas = lineasXml.Select(l => new
        {
            codigo = l.Element(cac + "Item")?.Element(cac + "SellersItemIdentification")?.Element(cbc + "ID")?.Value?.Trim(),
            descripcion = l.Element(cac + "Item")?.Element(cbc + "Description")?.Value?.Trim() ?? "Producto",
            cantidad = Num(l.Element(cbc + "InvoicedQuantity")?.Value),
            precioUnitario = Num(l.Element(cac + "Price")?.Element(cbc + "PriceAmount")?.Value),
        });

        return Ok(new
        {
            message = "XML leído correctamente",
            data = new
            {
                numeroDocumentoXml = root.Element(cbc + "ID")?.Value,
                fechaEmision = root.Element(cbc + "IssueDate")?.Value,
                clienteNumeroDocumento = cliente?.Element(cac + "PartyIdentification")?.Element(cbc + "ID")?.Value?.Trim(),
                clienteRazonSocial = (cliente?.Element(cac + "PartyLegalEntity")?.Element(cbc + "RegistrationName")?.Value
                                      ?? cliente?.Element(cac + "PartyName")?.Element(cbc + "Name")?.Value)?.Trim(),
                clienteDireccion = cliente?.Element(cac + "PartyLegalEntity")?.Element(cac + "RegistrationAddress")
                                         ?.Element(cac + "AddressLine")?.Element(cbc + "Line")?.Value?.Trim(),
                lineas
            }
        });
    }

    [HttpPost("crear-nota")]
    public async Task<IActionResult> CrearNotaCreditoDebito(NotaPayload payload) => Ok(await _comprobanteService.CrearNotaCreditoDebito(payload));

    [HttpGet("buscar")]
    public async Task<IActionResult> BuscarComprobante([FromQuery] string serie, [FromQuery] int correlativo) => Ok(await _comprobanteService.BuscarComprobantePorSerieCorrelativo(serie, correlativo));

    [HttpGet("listar")]
    public async Task<IActionResult> ListarComprobantes([FromQuery] ComprobanteQueryParams queryparam) => Ok(await _comprobanteService.ListarComprobantes(queryparam));

    [HttpGet("ventas-realizadas")]
    public async Task<IActionResult> VentasRealizadas([FromQuery] string fecha) => Ok(await _comprobanteService.VentasRealizadas(fecha));

    [HttpPost("anular")]
    public async Task<IActionResult> AnularVenta([FromBody] ComprobanteAnulacionPayload payload) => Ok(await _comprobanteService.AnularVenta(payload));

    [HttpGet("generar-pdf")]
    public async Task<IActionResult> GenerarPdf([FromQuery] int idComprobante) => Ok(await _comprobanteService.GenerarPdf(idComprobante));

    [HttpGet("listar-fichas")]
    public async Task<IActionResult> ListarFichas([FromQuery] string fecha) => Ok(await _rentaService.ListarFichas(fecha));

    [HttpPut("modificar-fecha-venta/{id}")]
    public async Task<IActionResult> ActualizarFechaVenta(int id, [FromBody] ActualizarFechaPayload payload) => Ok(await _comprobanteService.ActualizarFechaVenta(id, payload.Fecha));

    [HttpGet("cotizacion/{id}/convertir")]
    public async Task<IActionResult> ObtenerCotizacionParaConvertir(int id) => Ok(await _comprobanteService.ObtenerCotizacionParaConvertir(id));

    [HttpGet("cotizacion/{id}/pdf")]
    public async Task<IActionResult> GenerarPdfCotizacion(int id) => Ok(await _comprobanteService.GenerarPdfCotizacion(id));

    [HttpGet("series")]
    public async Task<IActionResult> ObtenerSeriesDocumento() => Ok(await _comprobanteService.ObtenerSeriesDocumento());

}


