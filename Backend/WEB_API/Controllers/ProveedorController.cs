using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/proveedor")]
[ApiController]
public class ProveedorController : ControllerBase
{
    private readonly IProveedorService proveedorService;

    public ProveedorController(IProveedorService proveedorService)
    {
        this.proveedorService = proveedorService;
    }
    [HttpPost("crear")]
    public async Task<IActionResult> CrearProveedor([FromBody] CreateProveedorPayload payload) => Ok(await proveedorService.CrearProveedor(payload));

    [HttpPut("modificar")]
    public async Task<IActionResult> ModificarProveedor([FromBody] UpdateProveedorPayload payload) => Ok(await proveedorService.ModificarProveedor(payload));
    
    [HttpDelete("eliminar")]
    public async Task<IActionResult> EliminarProveedor([FromQuery] int IdProducto) => Ok(await proveedorService.EliminarProveedor(IdProducto));

    [HttpGet("listar")]
    public async Task<IActionResult> GetListarProveedor([FromQuery] ProveedorPayload payload) => Ok(await proveedorService.ListarProveedor(payload));

}


