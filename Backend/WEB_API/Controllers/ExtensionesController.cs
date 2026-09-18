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

    [HttpGet("motivos-nota")]
    public async Task<IActionResult> motivosNota([FromQuery] int tipoDocumentoVentaId) => Ok(await _extensionesService.ListarMotivosNota(tipoDocumentoVentaId));

    [HttpGet("tipos-igv")]
    public async Task<IActionResult> tiposIgv() => Ok(await _extensionesService.ListarTiposIgv());

    [HttpGet("unidades-medida")]
    public async Task<IActionResult> unidadesMedida() => Ok(await _extensionesService.ListarUnidadesMedida());

    [HttpGet("tipos-operacion")]
    public async Task<IActionResult> tiposOperacion() => Ok(await _extensionesService.ListarTiposOperacion());

    [HttpGet("monedas")]
    public async Task<IActionResult> monedas() => Ok(await _extensionesService.ListarMonedas());

    // Piloto de enforcement real de permisos (ver SubmoduloAuthorizationHandler): a diferencia
    // del resto de endpoints admin de este controller (MetodoPago, Salon, Sucursal -- ninguno
    // restringido por rol/submodulo hoy), estos 20 exigen el submodulo "1401" (Catalogos de
    // Documentos) via el claim "rutas" del JWT, no solo estar autenticado.
    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpGet("tipos-igv/listar")]
    public async Task<IActionResult> ListarTiposIgvAdmin() => Ok(await _extensionesService.ListarTiposIgvAdmin());

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPost("tipos-igv/crear")]
    public async Task<IActionResult> CrearTipoIgv([FromBody] CreateTipoIgvPayload payload) => Ok(await _extensionesService.CrearTipoIgv(payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("tipos-igv/{id}")]
    public async Task<IActionResult> ActualizarTipoIgv(int id, [FromBody] UpdateTipoIgvPayload payload) => Ok(await _extensionesService.ActualizarTipoIgv(id, payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("tipos-igv/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoTipoIgv(int id, [FromBody] SetEstadoPayload payload) => Ok(await _extensionesService.CambiarEstadoTipoIgv(id, payload.Estado));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpGet("unidades-medida/listar")]
    public async Task<IActionResult> ListarUnidadesMedidaAdmin() => Ok(await _extensionesService.ListarUnidadesMedidaAdmin());

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPost("unidades-medida/crear")]
    public async Task<IActionResult> CrearUnidadMedida([FromBody] CreateUnidadMedidaPayload payload) => Ok(await _extensionesService.CrearUnidadMedida(payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("unidades-medida/{id}")]
    public async Task<IActionResult> ActualizarUnidadMedida(int id, [FromBody] UpdateUnidadMedidaPayload payload) => Ok(await _extensionesService.ActualizarUnidadMedida(id, payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("unidades-medida/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoUnidadMedida(int id, [FromBody] SetEstadoPayload payload) => Ok(await _extensionesService.CambiarEstadoUnidadMedida(id, payload.Estado));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpGet("tipos-operacion/listar")]
    public async Task<IActionResult> ListarTiposOperacionAdmin() => Ok(await _extensionesService.ListarTiposOperacionAdmin());

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPost("tipos-operacion/crear")]
    public async Task<IActionResult> CrearTipoOperacion([FromBody] CreateTipoOperacionPayload payload) => Ok(await _extensionesService.CrearTipoOperacion(payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("tipos-operacion/{id}")]
    public async Task<IActionResult> ActualizarTipoOperacion(int id, [FromBody] UpdateTipoOperacionPayload payload) => Ok(await _extensionesService.ActualizarTipoOperacion(id, payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("tipos-operacion/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoTipoOperacion(int id, [FromBody] SetEstadoPayload payload) => Ok(await _extensionesService.CambiarEstadoTipoOperacion(id, payload.Estado));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpGet("motivos-nota/listar")]
    public async Task<IActionResult> ListarMotivosNotaAdmin() => Ok(await _extensionesService.ListarMotivosNotaAdmin());

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPost("motivos-nota/crear")]
    public async Task<IActionResult> CrearMotivoNota([FromBody] CreateMotivoNotaPayload payload) => Ok(await _extensionesService.CrearMotivoNota(payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("motivos-nota/{id}")]
    public async Task<IActionResult> ActualizarMotivoNota(int id, [FromBody] UpdateMotivoNotaPayload payload) => Ok(await _extensionesService.ActualizarMotivoNota(id, payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("motivos-nota/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoMotivoNota(int id, [FromBody] SetEstadoPayload payload) => Ok(await _extensionesService.CambiarEstadoMotivoNota(id, payload.Estado));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpGet("monedas/listar")]
    public async Task<IActionResult> ListarMonedasAdmin() => Ok(await _extensionesService.ListarMonedasAdmin());

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPost("monedas/crear")]
    public async Task<IActionResult> CrearMoneda([FromBody] CreateMonedaPayload payload) => Ok(await _extensionesService.CrearMoneda(payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("monedas/{id}")]
    public async Task<IActionResult> ActualizarMoneda(int id, [FromBody] UpdateMonedaPayload payload) => Ok(await _extensionesService.ActualizarMoneda(id, payload));

    [Authorize(Policy = "CatalogosDocumentosAdmin")]
    [HttpPut("monedas/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoMoneda(int id, [FromBody] SetEstadoPayload payload) => Ok(await _extensionesService.CambiarEstadoMoneda(id, payload.Estado));

    [HttpGet("ruc/{ruc}")]
    public async Task<IActionResult> consultarRuc(string ruc) => Ok(await _extensionesService.ConsultarRuc(ruc));

    [HttpGet("dni/{dni}")]
    public async Task<IActionResult> consultarDni(string dni) => Ok(await _extensionesService.ConsultarDni(dni));

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

    [HttpGet("paises")]
    public async Task<IActionResult> paises() => Ok(await _extensionesService.ListarPaises());

    [HttpGet("sucursales")]
    public async Task<IActionResult> sucursales() => Ok(await _extensionesService.ListarSucursales());

    // AllowAnonymous: catálogo nacional de ubigeos (sin TenantId/SucursalId), lo necesita
    // el formulario del pedido público (/pedido/{token}) donde el cliente aún no tiene sesión.
    [AllowAnonymous]
    [HttpGet("ubigeos")]
    public async Task<IActionResult> ubigeos() => Ok(await _extensionesService.ListarUbigeos());

    // AllowAnonymous: mismo motivo que ubigeos, lo consume el formulario público de pedido.
    [AllowAnonymous]
    [HttpGet("salones")]
    public async Task<IActionResult> salones([FromQuery] string ubigeoId) => Ok(await _extensionesService.ListarSalones(ubigeoId));

    [HttpGet("salones/listar")]
    public async Task<IActionResult> ListarSalonesAdmin() => Ok(await _extensionesService.ListarSalonesAdmin());

    [HttpPost("salones/crear")]
    public async Task<IActionResult> CrearSalon([FromBody] CreateSalonPayload payload) => Ok(await _extensionesService.CrearSalon(payload));

    [HttpPut("salones/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoSalon(int id, [FromBody] SetEstadoPayload payload) => Ok(await _extensionesService.CambiarEstadoSalon(id, payload.Estado));

    [HttpPost("crear-sucursal")]
    public async Task<IActionResult> CrearSucursal([FromBody] CreateSucursalPayload payload) => Ok(await _extensionesService.CrearSucursal(payload));

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("sucursales/{sucursalId}/tenant")]
    public async Task<IActionResult> ReasignarTenantSucursal(int sucursalId, [FromBody] ReasignarTenantSucursalPayload payload) => Ok(await _extensionesService.ReasignarTenantSucursal(sucursalId, payload.TenantKey));

}


