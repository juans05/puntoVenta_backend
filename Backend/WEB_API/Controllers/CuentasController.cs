using Application.Services;
using Domain.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

// Cuentas por cobrar (clientes) y por pagar (proveedores): mismo formato, dos lados.
[ApiController]
public class CuentasController : ControllerBase
{
    private readonly CuentasService _service;

    public CuentasController(CuentasService service)
    {
        _service = service;
    }

    // ---- Por cobrar ----
    [HttpGet("api/cuentas-por-cobrar/resumen")]
    public async Task<IActionResult> ResumenCobrar() => Ok(await _service.Resumen(true));

    [HttpGet("api/cuentas-por-cobrar/cliente/{id}/documentos")]
    public async Task<IActionResult> DocumentosCobrar(int id) => Ok(await _service.Documentos(true, id));

    [HttpGet("api/cuentas-por-cobrar/antiguedad")]
    public async Task<IActionResult> AntiguedadCobrar() => Ok(await _service.Antiguedad(true));

    [HttpPost("api/cobranzas/crear")]
    public async Task<IActionResult> CrearCobranza([FromBody] RegistrarPagoCuentaPayload payload) => Ok(await _service.RegistrarPago(true, payload));

    [HttpGet("api/cobranzas/listar")]
    public async Task<IActionResult> ListarCobranzas([FromQuery] PagosCuentaQueryParams payload) => Ok(await _service.ListarPagos(true, payload));

    [HttpPut("api/cobranzas/{id}/anular")]
    public async Task<IActionResult> AnularCobranza(int id) => Ok(await _service.AnularPago(true, id));

    // ---- Por pagar ----
    [HttpGet("api/cuentas-por-pagar/resumen")]
    public async Task<IActionResult> ResumenPagar() => Ok(await _service.Resumen(false));

    [HttpGet("api/cuentas-por-pagar/proveedor/{id}/documentos")]
    public async Task<IActionResult> DocumentosPagar(int id) => Ok(await _service.Documentos(false, id));

    [HttpGet("api/cuentas-por-pagar/antiguedad")]
    public async Task<IActionResult> AntiguedadPagar() => Ok(await _service.Antiguedad(false));

    [HttpPost("api/pagos-proveedor/crear")]
    public async Task<IActionResult> CrearPagoProveedor([FromBody] RegistrarPagoCuentaPayload payload) => Ok(await _service.RegistrarPago(false, payload));

    [HttpGet("api/pagos-proveedor/listar")]
    public async Task<IActionResult> ListarPagosProveedor([FromQuery] PagosCuentaQueryParams payload) => Ok(await _service.ListarPagos(false, payload));

    [HttpPut("api/pagos-proveedor/{id}/anular")]
    public async Task<IActionResult> AnularPagoProveedor(int id) => Ok(await _service.AnularPago(false, id));
}
