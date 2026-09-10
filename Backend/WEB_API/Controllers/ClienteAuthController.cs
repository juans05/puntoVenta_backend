using Application.Interfaces.IServices;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[Route("api/cliente/auth")]
[ApiController]
[AllowAnonymous]
public class ClienteAuthController : ControllerBase
{
    private readonly IClienteAuthService _clienteAuthService;

    public ClienteAuthController(IClienteAuthService clienteAuthService)
    {
        _clienteAuthService = clienteAuthService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] ClienteLoginPayload payload)
        => Ok(await _clienteAuthService.Login(payload));
}

[Route("api/cliente")]
[ApiController]
[Authorize(Policy = "Cliente")]
public class ClientePortalController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public ClientePortalController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpGet("pedidos")]
    public async Task<IActionResult> ListarPedidos([FromQuery] PaginationPayload payload)
    {
        var clienteIdClaim = User.FindFirst("clienteId")?.Value;
        if (string.IsNullOrEmpty(clienteIdClaim) || !int.TryParse(clienteIdClaim, out var clienteId))
            return Unauthorized();

        return Ok(await _pedidoService.ListarPedidosCliente(clienteId, payload));
    }

    [HttpGet("pedidos/{id}")]
    public async Task<IActionResult> ObtenerPedido(int id)
    {
        var clienteIdClaim = User.FindFirst("clienteId")?.Value;
        if (string.IsNullOrEmpty(clienteIdClaim) || !int.TryParse(clienteIdClaim, out var clienteId))
            return Unauthorized();

        return Ok(await _pedidoService.ObtenerPedidoCliente(clienteId, id));
    }
}