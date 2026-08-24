using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/renta")]
[ApiController]
public class RentaController : ControllerBase
{
    private readonly IRentaService _rentaService;

    public RentaController(IRentaService rentaService)
    {
        _rentaService = rentaService;
    }

    [HttpGet("listar-rentas")]
    public async Task<IActionResult> ListarRentas([FromQuery] string fecha, [FromQuery] string turno)
        => Ok(await _rentaService.ListarRentas(fecha, turno));

    [HttpGet("listar-cuartos")]
    public async Task<IActionResult> ListarRecursos()
        => Ok(await _rentaService.ListarRecursos());

    [HttpGet("listar-cuartos-copados")]
    public async Task<IActionResult> ListarRecursosCopados([FromQuery] string turno)
        => Ok(await _rentaService.ListarRecursosCopados(turno));

    [HttpGet("configuracion")]
    public async Task<IActionResult> ObtenerConfiguracion()
        => Ok(await _rentaService.ObtenerConfiguracion());

    [HttpPut("configuracion")]
    public async Task<IActionResult> ActualizarConfiguracion([FromBody] ConfiguracionRentaPayload payload)
        => Ok(await _rentaService.ActualizarConfiguracion(payload));

    [HttpPost("crear-renta")]
    public async Task<IActionResult> CrearRenta([FromBody] CreateRentaPayload payload)
        => Ok(await _rentaService.CrearRenta(payload));

    [HttpGet("reporte-rentas")]
    public async Task<IActionResult> ReporteRentas([FromQuery] string fecha, [FromQuery] string turno)
        => Ok(await _rentaService.ReporteRentas(fecha, turno));

    [HttpPut("marcar-salida")]
    public async Task<IActionResult> MarcarSalida([FromQuery] int anfitrionaId, [FromQuery] string turno)
        => Ok(await _rentaService.MarcarSalida(anfitrionaId, turno));

    [HttpPut("completar-renta")]
    public async Task<IActionResult> CompletarDeuda([FromQuery] int idRenta)
        => Ok(await _rentaService.CompletarDeuda(idRenta));
}