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

}


