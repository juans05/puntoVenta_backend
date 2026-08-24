using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/extensiones")]
[ApiController]
public class ExtensionesController : ControllerBase
{
    private readonly IExtensionesService _extensionesService;

    public ExtensionesController(IExtensionesService extensionesService)
    {
        _extensionesService = extensionesService;
    }


    [HttpGet("tipo-documento")]
    public async Task<IActionResult> tipoDocumento() => Ok(await _extensionesService.ListarTipoDocumento());

    [HttpGet("tipo-documento-venta")]
    public async Task<IActionResult> tipoDocumentoVenta() => Ok(await _extensionesService.ListarTipoDocumentoVenta());

    [HttpGet("tipo-metodo-pago")]
    public async Task<IActionResult> metodoPago() => Ok(await _extensionesService.ListarMetodoPago());

    [HttpGet("metodo-pago/listar")]
    public async Task<IActionResult> ListarMetodoPagoAdmin() => Ok(await _extensionesService.ListarMetodoPagoAdmin());

    [HttpPost("metodo-pago/crear")]
    public async Task<IActionResult> CrearMetodoPago([FromBody] CreateMetodoPagoPayload payload) => Ok(await _extensionesService.CrearMetodoPago(payload));

    [HttpPut("metodo-pago/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoMetodoPago(int id, [FromBody] SetEstadoPayload payload) => Ok(await _extensionesService.CambiarEstadoMetodoPago(id, payload.Estado));

    [HttpGet("nacionalidad")]
    public async Task<IActionResult> nacionalidad() => Ok(await _extensionesService.ListarNacionalidad());

    [HttpGet("rubros")]
    public async Task<IActionResult> rubros() => Ok(await _extensionesService.ListarRubros());

    [HttpGet("sucursales")]
    public async Task<IActionResult> sucursales() => Ok(await _extensionesService.ListarSucursales());

    [HttpGet("ubigeos")]
    public async Task<IActionResult> ubigeos() => Ok(await _extensionesService.ListarUbigeos());

    [HttpPost("crear-sucursal")]
    public async Task<IActionResult> CrearSucursal([FromBody] CreateSucursalPayload payload) => Ok(await _extensionesService.CrearSucursal(payload));

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("sucursales/{sucursalId}/tenant")]
    public async Task<IActionResult> ReasignarTenantSucursal(int sucursalId, [FromBody] ReasignarTenantSucursalPayload payload) => Ok(await _extensionesService.ReasignarTenantSucursal(sucursalId, payload.TenantKey));

}


