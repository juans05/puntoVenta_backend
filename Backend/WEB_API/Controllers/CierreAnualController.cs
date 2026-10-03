using Application.Services;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[ApiController]
[Route("api/cierre-anual")]
public class CierreAnualController : ControllerBase
{
    private readonly CierreAnualService _service;

    public CierreAnualController(CierreAnualService service)
    {
        _service = service;
    }

    [HttpPost("validar")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> Validar([FromBody] CierreAnualPayload payload) => Ok(await _service.Validar(payload));

    [HttpPost("ejecutar")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> Ejecutar([FromBody] CierreAnualPayload payload) => Ok(await _service.Ejecutar(payload));
}
