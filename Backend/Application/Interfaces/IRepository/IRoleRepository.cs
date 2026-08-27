using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IRoleRepository
{
    Task<(ServiceStatus, List<RoleDto>?, string)> ListarRoles();
    Task<(ServiceStatus, RoleDto?, string)> ObtenerRol(string id);
    Task<(ServiceStatus, RoleDto?, string)> CrearRol(CreateRolePayload payload);
    Task<(ServiceStatus, RoleDto?, string)> ActualizarRol(string id, UpdateRolePayload payload);
    Task<(ServiceStatus, string)> EliminarRol(string id);
    Task<(ServiceStatus, List<AccesosDetalle>?, string)> ObtenerCatalogoSubmodulos();
    Task<(ServiceStatus, string)> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload);
    Task<(List<AccesosDetalle> Rutas, string? RutaPorDefecto)> ResolverAccesoUsuario(IList<string> nombresDeRol);
}
