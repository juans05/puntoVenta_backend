using Application.Interfaces.IServices;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _usersService;

        public UserController(IUserService userService)
        {
            _usersService = userService;
        }
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserPayload payload) => Ok(await _usersService.CreateUserAsync(payload));

        [HttpGet("get-all-user-access/{username}")]
        public async Task<IActionResult> GetAllCategories(string username) => Ok(await _usersService.GetAllUserAccess(username));

        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers() => Ok(await _usersService.GetAllUsers());

        [HttpGet("listar-usuarios")]
        public async Task<IActionResult> ListarUsuarios() => Ok(await _usersService.ListarUsuarios());

        // Reutiliza la policy de Roles y Permisos (submodulo 1402): activar/desactivar usuarios
        // es administracion de cuentas, no algo que cualquier usuario autenticado del tenant deba poder hacer.
        [Authorize(Policy = "RolesPermisosAdmin")]
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstadoUsuario(string id, [FromBody] CambiarEstadoUsuarioPayload payload) =>
            Ok(await _usersService.CambiarEstadoUsuario(id, payload.Activo));

    }
}
