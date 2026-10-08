using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

// Libro diario: los asientos se generan desde codigo interno
// (OrdenCompraRepository/CompraRepository/CuentasRepository via IAsientoContableRepository).
[ApiController]
[Route("api/asientos-contables")]
public class AsientoContableController : ControllerBase
{
    private readonly AsientoContableService _service;

    public AsientoContableController(AsientoContableService service)
    {
        _service = service;
    }

    [HttpGet("listar")]
    public async Task<IActionResult> Listar([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? origenTipo)
        => Ok(await _service.Listar(desde, hasta, origenTipo));

    [HttpGet("estado-resultados")]
    public async Task<IActionResult> ObtenerEstadoResultados([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => Ok(await _service.ObtenerEstadoResultados(desde, hasta));

    [HttpGet("balance-general")]
    public async Task<IActionResult> ObtenerBalanceGeneral([FromQuery] DateTime? hasta)
        => Ok(await _service.ObtenerBalanceGeneral(hasta ?? DateTime.UtcNow.AddHours(-5)));

    // Asiento manual: unico punto donde el usuario escribe en el libro diario. Anular = reverso (nunca borra).
    [Authorize(Policy = "RolesPermisosAdmin")]
    [HttpPost("crear")]
    public async Task<IActionResult> CrearManual([FromBody] Domain.Payloads.CrearAsientoManualPayload payload)
        => Ok(await _service.CrearManual(payload));

    [Authorize(Policy = "RolesPermisosAdmin")]
    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> AnularManual(int id)
        => Ok(await _service.AnularManual(id));
}
