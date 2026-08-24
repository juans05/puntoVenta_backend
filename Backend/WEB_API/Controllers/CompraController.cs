using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/compras")]
[ApiController]
public class CompraController : ControllerBase
{
    private readonly ICompraService _compraService;

    public CompraController(ICompraService compraService)
    {
        _compraService = compraService;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearCompra([FromBody] CreateCompraPayload payload) => Ok(await _compraService.CrearCompra(payload));

    [HttpPut("anular")]
    public async Task<IActionResult> AnularCompra([FromQuery] int id) => Ok(await _compraService.AnularCompra(id));

    [HttpGet("listar")]
    public async Task<IActionResult> ListarCompras([FromQuery] CompraQueryParams payload) => Ok(await _compraService.ListarCompras(payload));

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerCompra(int id) => Ok(await _compraService.ObtenerCompra(id));
}