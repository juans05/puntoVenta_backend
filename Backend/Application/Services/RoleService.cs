using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<MessageResult<object>> ListarRoles()
    {
        var (estado, resp, message) = await _roleRepository.ListarRoles();
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, resp);
        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<object>> ObtenerRol(string id)
    {
        var (estado, resp, message) = await _roleRepository.ObtenerRol(id);
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : HttpStatusCode.InternalServerError, message, resp);
        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<object>> CrearRol(CreateRolePayload payload)
    {
        var (estado, resp, message) = await _roleRepository.CrearRol(payload);
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<object>> ActualizarRol(string id, UpdateRolePayload payload)
    {
        var (estado, resp, message) = await _roleRepository.ActualizarRol(id, payload);
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<bool>> EliminarRol(string id)
    {
        var (estado, message) = await _roleRepository.EliminarRol(id);
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, null);
        return MessageResult<bool>.Of(message, true);
    }

    public async Task<MessageResult<object>> ObtenerCatalogoSubmodulos()
    {
        var (estado, resp, message) = await _roleRepository.ObtenerCatalogoSubmodulos();
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, resp);
        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<bool>> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload)
    {
        var (estado, message) = await _roleRepository.AsignarRolesAUsuario(payload);
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, null);
        return MessageResult<bool>.Of(message, true);
    }

    public async Task<MessageResult<object>> ObtenerRolesDeUsuario(string userId)
    {
        var (estado, resp, message) = await _roleRepository.ObtenerRolesDeUsuario(userId);
        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, resp);
        return MessageResult<object>.Of(message, resp);
    }
}
