using Application.Services;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[ApiController]
[Route("api/departamentos")]
public class DepartamentoController : ControllerBase
{
    private readonly DepartamentoService _service;

    public DepartamentoController(DepartamentoService service)
    {
        _service = service;
    }

    [HttpPost("crear")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> Crear([FromBody] CrearDepartamentoPayload payload) => Ok(await _service.Crear(payload));

    [HttpPut("actualizar/{id}")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CrearDepartamentoPayload payload) => Ok(await _service.Actualizar(id, payload));

    [HttpGet("listar")]
    public async Task<IActionResult> Listar() => Ok(await _service.Listar());

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(int id) => Ok(await _service.Obtener(id));

    [HttpPut("{id}/aprobadores")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> AsignarAprobadores(int id, [FromBody] AsignarAprobadoresPayload payload) => Ok(await _service.AsignarAprobadores(id, payload));
}
