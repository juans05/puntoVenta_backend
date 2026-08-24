using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/gastos")]
[ApiController]
public class GastoController : ControllerBase
{
    private readonly IGastoService _gastoService;

    public GastoController(IGastoService gastoService)
    {
        _gastoService = gastoService;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearGasto([FromBody] CreateGastoPayload payload) => Ok(await _gastoService.CrearGasto(payload));

    [HttpPut("anular")]
    public async Task<IActionResult> AnularGasto([FromQuery] int id) => Ok(await _gastoService.AnularGasto(id));

    [HttpGet("listar")]
    public async Task<IActionResult> ListarGastos([FromQuery] GastoQueryParams payload) => Ok(await _gastoService.ListarGastos(payload));

    [HttpGet("categorias/listar")]
    public async Task<IActionResult> ListarCategorias() => Ok(await _gastoService.ListarCategorias());

    [HttpPost("categorias/crear")]
    public async Task<IActionResult> CrearCategoria([FromBody] CreateCategoriaGastoPayload payload) => Ok(await _gastoService.CrearCategoria(payload));

    [HttpPut("categorias/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoCategoria(int id, [FromBody] SetEstadoPayload payload) => Ok(await _gastoService.CambiarEstadoCategoria(id, payload.Estado));
}
