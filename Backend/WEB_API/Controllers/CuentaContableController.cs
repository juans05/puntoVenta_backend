using Application.Services;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[ApiController]
[Route("api/cuentas-contables")]
public class CuentaContableController : ControllerBase
{
    private readonly CuentaContableService _service;

    public CuentaContableController(CuentaContableService service)
    {
        _service = service;
    }

    [HttpPost("crear")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> Crear([FromBody] CrearCuentaContablePayload payload) => Ok(await _service.Crear(payload));

    [HttpPut("actualizar/{id}")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarCuentaContablePayload payload) => Ok(await _service.Actualizar(id, payload));

    [HttpGet("listar")]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInactivas = false) => Ok(await _service.Listar(incluirInactivas));

    [HttpGet("codigos-eeff-niif")]
    public async Task<IActionResult> CodigosEeffNiif() => Ok(await _service.ListarCodigosEeffNiif());

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(int id) => Ok(await _service.Obtener(id));
}
