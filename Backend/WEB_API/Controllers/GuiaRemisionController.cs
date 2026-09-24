using Application.Services;
using Domain.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[ApiController]
[Route("api/guias-remision")]
public class GuiaRemisionController : ControllerBase
{
    private readonly GuiaRemisionService _service;

    public GuiaRemisionController(GuiaRemisionService service)
    {
        _service = service;
    }

    [HttpPost("desde-entrega/{entregaId}")]
    public async Task<IActionResult> GenerarDesdeEntrega(int entregaId) => Ok(await _service.GenerarDesdeEntrega(entregaId));

    [HttpPut("{id}/actualizar")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarGuiaRemisionPayload payload) => Ok(await _service.Actualizar(id, payload));

    [HttpGet("listar")]
    public async Task<IActionResult> Listar([FromQuery] GuiaRemisionQueryParams payload) => Ok(await _service.Listar(payload));

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(int id) => Ok(await _service.Obtener(id));

    [HttpPut("{id}/anular")]
    public async Task<IActionResult> Anular(int id) => Ok(await _service.Anular(id));
}
