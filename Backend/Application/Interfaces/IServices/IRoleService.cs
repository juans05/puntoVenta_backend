using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IRoleService
{
    Task<MessageResult<object>> ListarRoles();
    Task<MessageResult<object>> ObtenerRol(string id);
    Task<MessageResult<object>> CrearRol(CreateRolePayload payload);
    Task<MessageResult<object>> ActualizarRol(string id, UpdateRolePayload payload);
    Task<MessageResult<bool>> EliminarRol(string id);
    Task<MessageResult<object>> ObtenerCatalogoSubmodulos();
    Task<MessageResult<bool>> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload);
    Task<MessageResult<object>> ObtenerRolesDeUsuario(string userId);
}
