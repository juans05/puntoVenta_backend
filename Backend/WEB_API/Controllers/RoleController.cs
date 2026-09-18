using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

// Gestion de Roles y Permisos: hoy la asignacion de submodulos era 100% automatica
// (TenantRepository.AsociarModuleUser le da TODO al admin de un tenant al crearlo, sin
// pantalla para ajustarla despues). Este controller expone el RoleRepository que ya existia
// en el codigo (con tests) pero nunca se conecto a un service/controller/DI.
[Authorize(Policy = "RolesPermisosAdmin")]
[Route("api/roles")]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("listar")]
    public async Task<IActionResult> ListarRoles() => Ok(await _roleService.ListarRoles());

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerRol(string id) => Ok(await _roleService.ObtenerRol(id));

    [HttpPost("crear")]
    public async Task<IActionResult> CrearRol([FromBody] CreateRolePayload payload) => Ok(await _roleService.CrearRol(payload));

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarRol(string id, [FromBody] UpdateRolePayload payload) => Ok(await _roleService.ActualizarRol(id, payload));

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarRol(string id) => Ok(await _roleService.EliminarRol(id));

    [HttpGet("catalogo-submodulos")]
    public async Task<IActionResult> ObtenerCatalogoSubmodulos() => Ok(await _roleService.ObtenerCatalogoSubmodulos());

    [HttpPost("asignar-usuario")]
    public async Task<IActionResult> AsignarRolesAUsuario([FromBody] AsignarRolesUsuarioPayload payload) => Ok(await _roleService.AsignarRolesAUsuario(payload));

    [HttpGet("usuario/{userId}")]
    public async Task<IActionResult> ObtenerRolesDeUsuario(string userId) => Ok(await _roleService.ObtenerRolesDeUsuario(userId));
}
