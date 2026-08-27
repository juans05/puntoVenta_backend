using Domain.DTO;
using Domain.Entities.Identity;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Application.Interfaces.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly SpaContext _context;
    private readonly RoleManager<Role> _roleManager;

    public RoleRepository(SpaContext context, RoleManager<Role> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<(ServiceStatus, List<RoleDto>?, string)> ListarRoles()
    {
        try
        {
            var roles = await _context.Roles.AsNoTracking()
                                             .Where(r => r.TenantId == _context.CurrentTenantName)
                                             .OrderBy(r => r.Prioridad)
                                             .ToListAsync();

            var lista = new List<RoleDto>();

            foreach (var role in roles)
            {
                var submoduleIds = await _context.RoleSubmodule.AsNoTracking()
                                                                .Where(rs => rs.RoleId == role.Id)
                                                                .Select(rs => rs.SubmoduleId)
                                                                .ToListAsync();

                var cantidadUsuarios = await _context.UserRoles.AsNoTracking()
                                                                .CountAsync(ur => ur.RoleId == role.Id);

                lista.Add(new RoleDto
                {
                    Id = role.Id,
                    Nombre = role.Name,
                    RutaPorDefecto = role.RutaPorDefecto,
                    Prioridad = role.Prioridad,
                    CantidadUsuarios = cantidadUsuarios,
                    SubmoduleIds = submoduleIds
                });
            }

            return (ServiceStatus.Ok, lista, "Roles listados correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar roles -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, RoleDto?, string)> ObtenerRol(string id)
    {
        try
        {
            var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
                return (ServiceStatus.NotFound, null, $"No se encontro el rol {id}");

            var submoduleIds = await _context.RoleSubmodule.AsNoTracking()
                                                            .Where(rs => rs.RoleId == role.Id)
                                                            .Select(rs => rs.SubmoduleId)
                                                            .ToListAsync();

            var cantidadUsuarios = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == role.Id);

            var dto = new RoleDto
            {
                Id = role.Id,
                Nombre = role.Name,
                RutaPorDefecto = role.RutaPorDefecto,
                Prioridad = role.Prioridad,
                CantidadUsuarios = cantidadUsuarios,
                SubmoduleIds = submoduleIds
            };

            return (ServiceStatus.Ok, dto, "Rol encontrado");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, RoleDto?, string)> CrearRol(CreateRolePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Nombre))
            return (ServiceStatus.FailedValidation, null, "El nombre del rol es obligatorio");

        var yaExiste = await _context.Roles.AsNoTracking()
                                            .AnyAsync(r => r.TenantId == _context.CurrentTenantName
                                                        && r.NormalizedName == payload.Nombre.ToUpper());

        if (yaExiste)
            return (ServiceStatus.FailedValidation, null, $"Ya existe un rol llamado '{payload.Nombre}'");

        try
        {
            var role = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = payload.Nombre,
                TenantId = _context.CurrentTenantName,
                RutaPorDefecto = payload.RutaPorDefecto,
                Prioridad = payload.Prioridad
            };

            var resultado = await _roleManager.CreateAsync(role);

            if (!resultado.Succeeded)
                return (ServiceStatus.FailedValidation, null, string.Join("; ", resultado.Errors.Select(e => e.Description)));

            foreach (var submoduleId in payload.SubmoduleIds.Distinct())
            {
                _context.RoleSubmodule.Add(new RoleSubmodule { RoleId = role.Id, SubmoduleId = submoduleId });
            }

            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, new RoleDto
            {
                Id = role.Id,
                Nombre = role.Name,
                RutaPorDefecto = role.RutaPorDefecto,
                Prioridad = role.Prioridad,
                CantidadUsuarios = 0,
                SubmoduleIds = payload.SubmoduleIds.Distinct().ToList()
            }, "Rol creado correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al crear el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, RoleDto?, string)> ActualizarRol(string id, UpdateRolePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Nombre))
            return (ServiceStatus.FailedValidation, null, "El nombre del rol es obligatorio");

        var role = await _context.Roles.AsTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return (ServiceStatus.NotFound, null, $"No se encontro el rol {id}");

        var nombreEnUso = await _context.Roles.AsNoTracking()
                                               .AnyAsync(r => r.Id != id
                                                           && r.TenantId == _context.CurrentTenantName
                                                           && r.NormalizedName == payload.Nombre.ToUpper());

        if (nombreEnUso)
            return (ServiceStatus.FailedValidation, null, $"Ya existe un rol llamado '{payload.Nombre}'");

        try
        {
            role.Name = payload.Nombre;
            role.NormalizedName = payload.Nombre.ToUpper();
            role.RutaPorDefecto = payload.RutaPorDefecto;
            role.Prioridad = payload.Prioridad;

            var existentes = _context.RoleSubmodule.Where(rs => rs.RoleId == id);
            _context.RoleSubmodule.RemoveRange(existentes);

            foreach (var submoduleId in payload.SubmoduleIds.Distinct())
            {
                _context.RoleSubmodule.Add(new RoleSubmodule { RoleId = id, SubmoduleId = submoduleId });
            }

            await _context.SaveChangesAsync();

            var cantidadUsuarios = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == id);

            return (ServiceStatus.Ok, new RoleDto
            {
                Id = role.Id,
                Nombre = role.Name,
                RutaPorDefecto = role.RutaPorDefecto,
                Prioridad = role.Prioridad,
                CantidadUsuarios = cantidadUsuarios,
                SubmoduleIds = payload.SubmoduleIds.Distinct().ToList()
            }, "Rol actualizado correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al actualizar el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> EliminarRol(string id)
    {
        var role = await _context.Roles.AsTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return (ServiceStatus.NotFound, $"No se encontro el rol {id}");

        var cantidadUsuarios = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == id);

        if (cantidadUsuarios > 0)
            return (ServiceStatus.FailedValidation, $"No se puede eliminar: el rol tiene {cantidadUsuarios} usuario(s) asignado(s)");

        try
        {
            var submodulos = _context.RoleSubmodule.Where(rs => rs.RoleId == id);
            _context.RoleSubmodule.RemoveRange(submodulos);
            await _context.SaveChangesAsync();

            var resultado = await _roleManager.DeleteAsync(role);

            if (!resultado.Succeeded)
                return (ServiceStatus.FailedValidation, string.Join("; ", resultado.Errors.Select(e => e.Description)));

            return (ServiceStatus.Ok, "Rol eliminado correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al eliminar el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, List<AccesosDetalle>?, string)> ObtenerCatalogoSubmodulos()
    {
        try
        {
            var submodulos = await _context.AspNetSubModule.AsNoTracking()
                                                            .Include(s => s.Module)
                                                            .ToListAsync();

            var agrupado = submodulos.GroupBy(s => s.ModuloId)
                                      .Select(g => new AccesosDetalle
                                      {
                                          Modulo = g.Key,
                                          ModuloNombre = g.First().Module.Nombre,
                                          SubModulos = g.Select(s => new SubModuloDetalle
                                          {
                                              SubModulo = s.Identificador,
                                              SubModuloNombre = s.Nombre
                                          }).ToList()
                                      })
                                      .OrderBy(g => g.Modulo)
                                      .ToList();

            return (ServiceStatus.Ok, agrupado, "Catalogo de submodulos");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener el catalogo -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload)
    {
        // Nota: User.TenantId es int y CurrentTenantName es el Tenant.Name (string, ej "SPASOLIS1"),
        // no son comparables directamente. Users no tiene HasQueryFilter global en SpaContext
        // (confirmado en OnModelCreating), asi que no hay scoping automatico por tenant aqui.
        // Este repositorio no recibe IHttpContextAccessor (a diferencia de UsersRepository.ListarUsuarios,
        // que resuelve el tenant del usuario autenticado via claims para comparar u.TenantId == appUser.TenantId).
        // Se resuelve el usuario solo por Id; la responsabilidad de validar que el UserId pertenezca al
        // tenant del caller queda en la capa de servicio/controller (Task 4), que si tiene acceso al contexto HTTP.
        var usuario = await _context.Users.AsNoTracking()
                                          .FirstOrDefaultAsync(u => u.Id == payload.UserId);

        if (usuario == null)
            return (ServiceStatus.NotFound, $"No se encontro el usuario {payload.UserId}");

        try
        {
            var roles = await _context.Roles.AsNoTracking()
                                             .Where(r => payload.RoleIds.Contains(r.Id))
                                             .Select(r => r.Id)
                                             .ToListAsync();

            var existentes = _context.UserRoles.Where(ur => ur.UserId == payload.UserId);
            _context.UserRoles.RemoveRange(existentes);
            await _context.SaveChangesRegularAsync();

            foreach (var roleId in roles)
            {
                _context.UserRoles.Add(new UserRol { UserId = payload.UserId, RoleId = roleId });
            }

            await _context.SaveChangesRegularAsync();

            return (ServiceStatus.Ok, "Roles asignados correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al asignar roles -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(List<AccesosDetalle> Rutas, string? RutaPorDefecto)> ResolverAccesoUsuario(IList<string> nombresDeRol)
    {
        if (nombresDeRol == null || nombresDeRol.Count == 0)
            return (new List<AccesosDetalle>(), null);

        var roles = await _context.Roles.AsNoTracking()
                                         .Where(r => nombresDeRol.Contains(r.Name))
                                         .OrderBy(r => r.Prioridad)
                                         .ToListAsync();

        var rutaPorDefecto = roles.FirstOrDefault()?.RutaPorDefecto;

        var roleIds = roles.Select(r => r.Id).ToList();

        var submoduleIds = await _context.RoleSubmodule.AsNoTracking()
                                                        .Where(rs => roleIds.Contains(rs.RoleId))
                                                        .Select(rs => rs.SubmoduleId)
                                                        .Distinct()
                                                        .ToListAsync();

        var submodulos = await _context.AspNetSubModule.AsNoTracking()
                                                        .Include(s => s.Module)
                                                        .Where(s => submoduleIds.Contains(s.Identificador))
                                                        .ToListAsync();

        var rutas = submodulos.GroupBy(s => s.ModuloId)
                               .Select(g => new AccesosDetalle
                               {
                                   Modulo = g.Key,
                                   ModuloNombre = g.First().Module.Nombre,
                                   SubModulos = g.Select(s => new SubModuloDetalle
                                   {
                                       SubModulo = s.Identificador,
                                       SubModuloNombre = s.Nombre
                                   }).ToList()
                               })
                               .ToList();

        return (rutas, rutaPorDefecto);
    }
}
