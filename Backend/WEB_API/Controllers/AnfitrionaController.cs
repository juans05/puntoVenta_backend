using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/anfitriona")]
[ApiController]
public class AnfitrionaController : ControllerBase
{
    private readonly IAnfitrionaService _anfitrionaService;

    public AnfitrionaController(IAnfitrionaService anfitrionaService)
    {
        _anfitrionaService = anfitrionaService;
    }

    [HttpGet("listar")]
    public async Task<IActionResult> ListarAnfitrionas([FromQuery] int Page, [FromQuery] int Amount)
        => Ok(await _anfitrionaService.ListarAnfitrionas(Page, Amount));

    [HttpPost("crear")]
    public async Task<IActionResult> CrearAnfitriona([FromBody] CreateAnfitrionaPayload payload)
        => Ok(await _anfitrionaService.CrearAnfitriona(payload));

    [HttpPut("modificar")]
    public async Task<IActionResult> ActualizarAnfitriona([FromBody] UpdateAnfitrionaPayload payload)
        => Ok(await _anfitrionaService.ActualizarAnfitriona(payload));

    [HttpDelete("eliminar")]
    public async Task<IActionResult> EliminarAnfitriona([FromQuery] int IdAnfitriona)
        => Ok(await _anfitrionaService.EliminarAnfitriona(IdAnfitriona));
}