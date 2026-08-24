using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/caja")]
[ApiController]
public class CajaController : ControllerBase
{
    private readonly ICajaService _cajaService;

    public CajaController(ICajaService cajaService)
    {
        _cajaService = cajaService;
    }

    [HttpGet("monto-actual")]
    public async Task<IActionResult> MontoActual([FromQuery] string usuario, [FromQuery] int? sucursalId = null) => Ok(await _cajaService.MontoActual(usuario, sucursalId));


    [HttpPost("abrir-caja")]
    public async Task<IActionResult> AbrirCaja([FromQuery] string monto, [FromQuery] int? sucursalId = null) => Ok(await _cajaService.AbrirCaja(monto, sucursalId));


    [HttpPost("cerrar-caja")]
    public async Task<IActionResult> CerrarCaja([FromQuery] int? sucursalId = null) => Ok(await _cajaService.CerrarCaja(sucursalId));

    [HttpGet("reporte-caja")]
    public async Task<IActionResult> ReporteCaja([FromQuery] string usuario, [FromQuery] string fecha, [FromQuery] int? sucursalId = null) => Ok(await _cajaService.ReporteCaja(usuario, fecha, sucursalId));


    [HttpGet("reporte-caja-resumido")]
    public async Task<IActionResult> ReporteCajaResumido([FromQuery] string usuario, [FromQuery] string fecha, [FromQuery] int? sucursalId = null) => Ok(await _cajaService.ReporteCajaResumido(usuario, fecha, sucursalId));


    [HttpGet("reporte-cierre-caja-vendedor")]
    public async Task<IActionResult> ReporteCajaResumido([FromQuery] string fecha, [FromQuery] int? sucursalId = null) => Ok(await _cajaService.HistoricoCierreCajaUsuario(fecha, sucursalId));

    [HttpPost("retiro")]
    public async Task<IActionResult> Retiro([FromBody] CreateRetiroPayload payload) => Ok(await _cajaService.Retiro(payload));

    [HttpGet("cajas")]
    public async Task<IActionResult> ListarCajas() => Ok(await _cajaService.ListarCajas());

    [HttpPost("crear-caja")]
    public async Task<IActionResult> CrearCaja([FromBody] CreateCajaPayload payload) => Ok(await _cajaService.CrearCaja(payload));
}


