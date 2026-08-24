using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/ingresos")]
[ApiController]
public class IngresoController : ControllerBase
{
    private readonly IIngresoService _ingresoService;

    public IngresoController(IIngresoService ingresoService)
    {
        _ingresoService = ingresoService;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearIngreso([FromBody] CreateIngresoPayload payload) => Ok(await _ingresoService.CrearIngreso(payload));

    [HttpPut("anular")]
    public async Task<IActionResult> AnularIngreso([FromQuery] int id) => Ok(await _ingresoService.AnularIngreso(id));

    [HttpGet("listar")]
    public async Task<IActionResult> ListarIngresos([FromQuery] IngresoQueryParams payload) => Ok(await _ingresoService.ListarIngresos(payload));
}