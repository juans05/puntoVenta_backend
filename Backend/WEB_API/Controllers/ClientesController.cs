using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/clientes")]
[ApiController]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CreateCliente(CreateClientePayload payload)

        => Ok(await _clienteService.CreateCliente(payload));


    [HttpPut("modificar")]
    public async Task<IActionResult> UpdateCliente([FromBody] UpdateClientePayload payload)

        => Ok(await _clienteService.UpdateCliente(payload));

    [HttpDelete("eliminar")]
    public async Task<IActionResult> EliminarProveedor([FromQuery] int idCliente)

        => Ok(await _clienteService.EliminarCliente(idCliente));

    [HttpGet("listar")]
    public async Task<IActionResult> GetClientes([FromQuery] ClientePayload payload)

        => Ok(await _clienteService.GetClientes(payload));

}


