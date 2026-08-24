using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/tenant")]
[ApiController]
public class TenantController : ControllerBase
{
    private readonly ITenantService tenantService;

    public TenantController(ITenantService tenantService)
    {
        this.tenantService = tenantService;
    }


    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("crear-tenant")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantPayload payload) => Ok(await tenantService.CreateTenant(payload.Nombre, payload.RubroId, payload.Configuracion));


    [AllowAnonymous]
    [HttpGet("configuracion-rubro")]
    public async Task<IActionResult> GetConfiguracionRubro([FromQuery] int rubroId) => Ok(await tenantService.GetConfiguracionRubro(rubroId));


    [HttpPut("configuracion-rubro")]
    public async Task<IActionResult> SaveConfiguracionRubro([FromQuery] int rubroId, [FromBody] ConfiguracionRentaPayload payload) => Ok(await tenantService.SaveConfiguracionRubro(rubroId, payload));


    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("crear-empresa")]
    public async Task<IActionResult> CreateEmpresa([FromBody] CreateEmpresaPayload payload) => Ok(await tenantService.CreateEmpresa(payload));


    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("add-empresa")]
    public async Task<IActionResult> AddEmpresa([FromBody] AddEmpresaPayload payload) => Ok(await tenantService.AddEmpresaTenant(payload));


    [AllowAnonymous]
    [HttpGet("empresa")]
    public async Task<IActionResult> GetEmpresa([FromQuery] string tenantNombre) => Ok(await tenantService.GetEmpresa(tenantNombre));


    [HttpPut("modificar")]
    public async Task<IActionResult> ModificarTenant([FromBody] UpdateTenantPayload payload) => Ok(await tenantService.ModificarTenant(payload));


    [AllowAnonymous]
    [HttpGet("all-tenants")]
    public async Task<IActionResult> GetAllTenants() => Ok(await tenantService.GetAllTenants());


    [HttpGet("listar")]
    public async Task<IActionResult> GetListarTenant() => Ok(await tenantService.ListarTenants());


    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("superadmin/listar")]
    public async Task<IActionResult> GetTenantsResumen() => Ok(await tenantService.GetTenantsResumen());


    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("superadmin/{identificador}/estado")]
    public async Task<IActionResult> SetTenantActivo(int identificador, [FromBody] SetTenantActivoPayload payload) => Ok(await tenantService.SetTenantActivo(identificador, payload.Activo));


    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("superadmin/{identificador}/reasignar-modulos")]
    public async Task<IActionResult> ReasignarModulos(int identificador) => Ok(await tenantService.ReasignarModulos(identificador));


    [AllowAnonymous]
    [HttpGet("recursos")]
    public async Task<IActionResult> GetRecursos()
    {
        var tenant = string.Empty;

        var tenantHost = HttpContext.Request.Headers.Origin.ToString();

        //return Ok(tenantHost);

        if (tenantHost.Contains("127") || tenantHost.Contains("localhost"))
        {
            tenant = HttpContext.Request.Headers["Tenant"].ToString();

            if (string.IsNullOrEmpty(tenant))
                return BadRequest("Se Debe enviar el tenant");
        }
        else
        {
            tenant = tenantHost.Split(".")[0];
            if (tenant.Contains("https://"))
                tenant = tenant.Replace("https://", "");
            else if (tenant.Contains("http://"))
                tenant = tenant.Replace("http://", "");
        }

        //codigo temporal hasta conseguir dominio

        return Ok(await tenantService.GetRecursos(tenant));
    }

}


