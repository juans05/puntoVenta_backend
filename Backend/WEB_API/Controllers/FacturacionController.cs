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


