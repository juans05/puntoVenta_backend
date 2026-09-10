using Application.Interfaces.IServices;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[Route("api/pedidos")]
[ApiController]
[Authorize(Policy = "Staff")]
public class PedidoController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidoController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpPost]
    public async Task<IActionResult> CrearPedido([FromBody] CreatePedidoPayload payload)
        => Ok(await _pedidoService.CrearPedido(payload));

    [HttpGet]
    public async Task<IActionResult> ListarPedidos([FromQuery] PedidoQueryParams payload)
        => Ok(await _pedidoService.ListarPedidos(payload));

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPedido(int id)
        => Ok(await _pedidoService.ObtenerPedidoPorId(id));

    [HttpPut("estado")]
    public async Task<IActionResult> ActualizarEstado([FromBody] UpdatePedidoEstadoPayload payload)
        => Ok(await _pedidoService.ActualizarEstadoPedido(payload));

    [HttpPost("reenviar-password")]
    public async Task<IActionResult> ReenviarPassword([FromBody] ReenviarPasswordPayload payload)
        => Ok(await _pedidoService.ReenviarPassword(payload.PedidoId));

    [HttpGet("{id}/etiqueta")]
    public async Task<IActionResult> ObtenerEtiqueta(int id)
        => Ok(await _pedidoService.ObtenerEtiquetaEnvio(id));
}

[Route("api/pedidos/publico")]
[ApiController]
[AllowAnonymous]
public class PedidoPublicoController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidoPublicoController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> ObtenerPedidoPublico(string token)
        => Ok(await _pedidoService.ObtenerPedidoPublico(token));

    [HttpPost("{token}")]
    public async Task<IActionResult> SubmitPedidoPublico(string token, [FromBody] PedidoPublicoSubmitPayload payload)
        => Ok(await _pedidoService.SubmitPedidoPublico(token, payload));
}