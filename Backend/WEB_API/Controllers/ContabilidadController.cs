using Application.Interfaces.IServices;
using Domain.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[Route("api/contabilidad")]
[ApiController]
public class ContabilidadController : ControllerBase
{
    private readonly IComprobanteService _comprobanteService;
    private readonly ICompraService _compraService;

    public ContabilidadController(IComprobanteService comprobanteService, ICompraService compraService)
    {
        _comprobanteService = comprobanteService;
        _compraService = compraService;
    }

    [HttpGet("libro-ventas")]
    public async Task<IActionResult> ObtenerLibroVentas([FromQuery] ContabilidadQueryParams payload) =>
        Ok(await _comprobanteService.ObtenerLibroVentas(payload));

    [HttpGet("reporte-detallado-ventas")]
    public async Task<IActionResult> ObtenerReporteDetalladoVentas([FromQuery] ContabilidadQueryParams payload) =>
        Ok(await _comprobanteService.ObtenerReporteDetalladoVentas(payload));

    [HttpGet("libro-compras")]
    public async Task<IActionResult> ObtenerLibroCompras([FromQuery] ContabilidadQueryParams payload) =>
        Ok(await _compraService.ObtenerLibroCompras(payload));

    [HttpGet("reporte-detallado-compras")]
    public async Task<IActionResult> ObtenerReporteDetalladoCompras([FromQuery] ContabilidadQueryParams payload) =>
        Ok(await _compraService.ObtenerReporteDetalladoCompras(payload));
}
