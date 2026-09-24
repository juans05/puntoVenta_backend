using Application.Services;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WEB_API.Controllers;

[ApiController]
public class OrdenCompraController : ControllerBase
{
    private readonly OrdenCompraService _service;

    public OrdenCompraController(OrdenCompraService service)
    {
        _service = service;
    }

    // ---- Configuracion del flujo (editar: solo administradores) ----

    [HttpGet("api/configuracion-flujo")]
    public async Task<IActionResult> ObtenerConfiguracion() => Ok(await _service.ObtenerConfiguracion());

    [HttpPut("api/configuracion-flujo")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> GuardarConfiguracion([FromBody] ConfiguracionFlujoPayload payload) => Ok(await _service.GuardarConfiguracion(payload));

    // ---- Ordenes de compra ----

    [HttpPost("api/ordenes-compra/crear")]
    public async Task<IActionResult> Crear([FromBody] CreateOrdenCompraPayload payload) => Ok(await _service.Crear(payload));

    [HttpPut("api/ordenes-compra/actualizar/{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CreateOrdenCompraPayload payload) => Ok(await _service.Actualizar(id, payload));

    [HttpGet("api/ordenes-compra/listar")]
    public async Task<IActionResult> Listar([FromQuery] OrdenCompraQueryParams payload) => Ok(await _service.Listar(payload));

    [HttpGet("api/ordenes-compra/{id}")]
    public async Task<IActionResult> Obtener(int id) => Ok(await _service.Obtener(id));

    [HttpPut("api/ordenes-compra/{id}/emitir")]
    public async Task<IActionResult> Emitir(int id) => Ok(await _service.Emitir(id));

    [HttpPut("api/ordenes-compra/{id}/aprobar")]
    [Authorize(Policy = "RolesPermisosAdmin")]
    public async Task<IActionResult> Aprobar(int id)
        => Ok(await _service.Aprobar(id, User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name));

    [HttpPut("api/ordenes-compra/{id}/anular")]
    public async Task<IActionResult> Anular(int id) => Ok(await _service.Anular(id));

    [HttpPut("api/ordenes-compra/{id}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] CerrarOrdenCompraPayload payload) => Ok(await _service.Cerrar(id, payload.Motivo));

    [HttpPost("api/ordenes-compra/{id}/recepciones")]
    public async Task<IActionResult> Recibir(int id, [FromBody] CreateRecepcionPayload payload) => Ok(await _service.Recibir(id, payload));

    [HttpPut("api/ordenes-compra/recepciones/{recepcionId}/anular")]
    public async Task<IActionResult> AnularRecepcion(int recepcionId) => Ok(await _service.AnularRecepcion(recepcionId));

    [HttpPost("api/ordenes-compra/{id}/facturar")]
    public async Task<IActionResult> Facturar(int id, [FromBody] FacturarOrdenCompraPayload payload) => Ok(await _service.Facturar(id, payload));
}
