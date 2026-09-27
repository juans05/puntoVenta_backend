using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

// Libro diario de solo lectura: los asientos se generan solo desde codigo interno
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
}
