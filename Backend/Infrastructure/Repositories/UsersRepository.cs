using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.DTO;
using Domain.Entities.Identity;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class UsersRepository : IUsersRepository
    {
        private readonly SpaContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;


        public UsersRepository(
            SpaContext context,
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }
        public async Task<(ServiceStatus, string, int)> CreateUser(CreateUserPayload payload)
        {
            if (payload.FirstName.Contains(" "))
            {
                var nombres = payload.FirstName.Split(" ");
                var count = 0;
                foreach (var item in nombres)
                {
                    nombres[count] = char.ToUpper(item[0]) + item.Substring(1);
                    count++;
                }
                payload.FirstName = string.Join(" ", nombres);
            }
            else
            {
                payload.FirstName = char.ToUpper(payload.FirstName[0]) + payload.FirstName.Substring(1);
            }

            if (payload.LastName.Contains(" "))
            {
                var apellidos = payload.LastName.Split(" ");
                var count = 0;
                foreach (var item in apellidos)
                {
                    apellidos[count] = char.ToUpper(item[0]) + item.Substring(1);
                    count++;
                }
                payload.LastName = string.Join(" ", apellidos);
            }
            else
            {
                payload.LastName = char.ToUpper(payload.LastName[0]) + payload.LastName.Substring(1);
            }


            // "username" es el claim que realmente emite AuthenticationRepository.DefaultClaims()
            // (ver Token()); el WS-Federation URI que se usaba antes ("http://schemas.xmlsoap.org/...")
            // nunca lo agrega nadie, asi que este chequeo fallaba con "Tenant Invalido" para
            // cualquier usuario autenticado -- mismo claim que ya usa TenantResolver.GetCurrentTenant().
            var claim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == "username");

            if (claim is null)
                return (ServiceStatus.FailedValidation, "Tenant Invalido", 4);

            // El claim "username" lleva NormalizedUserName (mayusculas, ver DefaultClaims en
            // AuthenticationRepository), no UserName -- comparar contra NormalizedUserName.
            var appUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.NormalizedUserName == claim.Value);


            //var tenantdb = await _context.Tenant.FirstOrDefaultAsync(x => x.Name == claim.Value);

            //if (tenantdb is null)
            //    return (ServiceStatus.FailedValidation, "Tenant No existe", 5);


            try
            {
                var entry = new User
                {
                    FirstName = payload.FirstName,
                    LastName = payload.LastName,
                    Email = payload.Email,
                    UserName = payload.UserName.ToUpper(),
                    PhoneNumber = payload.Phone,
                    Celular = payload.Celular,
                    SucursalId = payload.SucursalId,
                    EmailConfirmed = true,
                    AvatarUrl = payload.Avatar,
                    FechaCreacion = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss"),
                    Estado = true,
                    TenantId = appUser.TenantId
                };

                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user != null) return (ServiceStatus.FailedValidation, $"El correo: {payload.Email} ya fue registrado", 110);

                var response = await _userManager.CreateAsync(entry, payload.Password);

                if (response.Succeeded)
                {
                    // "Administrador" = acceso completo de inmediato (mismo criterio que ya usa el
                    // sistema para el admin fundador de un tenant). Un rol especifico (creado en
                    // Roles y Permisos) le da acceso restringido. Sin ninguno de los dos, queda sin
                    // acceso hasta que alguien se lo asigne despues -- comportamiento ya existente.
                    if (payload.EsAdministrador)
                        await AsociarTodosLosSubmodulos(entry.Id, _context.CurrentTenantName);
                    else if (!string.IsNullOrWhiteSpace(payload.RoleId))
                        await _context.UserRoles.AddAsync(new UserRol { UserId = entry.Id, RoleId = payload.RoleId });

                    if (payload.EsAdministrador || !string.IsNullOrWhiteSpace(payload.RoleId))
                        await _context.SaveChangesRegularAsync();

                    return (ServiceStatus.Ok, "Cuenta creada exitosamente", 1);
                }
                else
                {
                    var result = response.Errors.ToList().Select(q => q.Description).ToArray()[0];
                    int code = 0;
                    if (result.Contains("Passwords must be at least")) code = 111;
                    else if (result.Contains("is already taken")) code = 112;
                    else
                    {
                        code = 999;
                        result = "Error no Capturado";
                    };
                    return (ServiceStatus.FailedValidation, $"{result}", code);
                }
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, $"Error Interno de la aplicación... {ex.Message}", 0);
            }

        }

        // Mismo criterio que TenantRepository.AsociarModuleUser (le da TODO el catalogo de
        // submodulos al admin fundador de un tenant) -- duplicado aqui porque esta clase no tiene
        // referencia a TenantRepository y el metodo original es privado.
        private async Task AsociarTodosLosSubmodulos(string userId, string tenant)
        {
            var fechaRegistro = DateTime.UtcNow.AddHours(-5);

            var submoduleIds = await _context.AspNetSubModule.AsNoTracking().Select(s => s.Identificador).ToListAsync();

            var nuevos = submoduleIds.Select(submoduleId => new AspNetUserSubModule
            {
                UserId = userId,
                SubmoduleId = submoduleId,
                UsuarioCreacion = "ADMIN",
                FechaCreacion = fechaRegistro,
                Estado = true,
                TenantId = tenant
            }).ToList();

            await _context.AspNetUserSubModule.AddRangeAsync(nuevos);
        }

        public async Task<object> GetModules()
        {
            var paginas = await _context.AspNetModule.AsNoTracking()
                                               .Select(q => new
                                               {
                                                   id = q.Identificador,
                                                   nombre = q.Nombre
                                               }).OrderByDescending(r => r.id)
                                                .ToListAsync();

            return paginas;
        }

        public async Task<object> GetCategories()
        {
            var paginas = await _context.AspNetSubModule.AsNoTracking()
                                                .Select(q => new
                                                {
                                                    id = q.Id,
                                                    nombre = q.Nombre,
                                                    modulo = q.Module.Nombre
                                                }).OrderBy(r => r.id)
                                                .ToListAsync();

            return paginas;
        }

        public async Task<object> GetAllCategories()
        {
            var paginas = await _context.AspNetSubModule.AsNoTracking()
                                                  .GroupBy(p => p.ModuloId)
                                                  .Select(q => new
                                                  {
                                                      modulo = q.Key,
                                                      nombremodulo = q.Select(s => s.Module.Nombre).FirstOrDefault(),
                                                      categoria = q.Select(p => new
                                                      {
                                                          codigo = p.Id,
                                                          nombrecategoria = p.Nombre
                                                      }).ToList()
                                                  }).ToListAsync();

            return paginas;
        }


        public async Task<(ServiceStatus, AuthenticatedUsuarioDto?, string)> GetAllUserAccess(string username)
        {
            List<string> roles = new List<string>();
            try
            {

                var user = await FindByUsername(username);

                if (user == null)
                    return (ServiceStatus.FailedValidation, null, "Usuario no encontrado");

                var applicationUserDto = _mapper.Map<ApplicationUserDto>(user);

                var rutas = applicationUserDto.Resumen;

                if (_userManager.SupportsUserRole)
                {
                    IList<string> userRoles = await _userManager.GetRolesAsync(user);
                    foreach (string role in userRoles)
                    {
                        roles.Add(role);
                    }
                }

                AuthenticatedUsuarioDto authenticatedUser = new AuthenticatedUsuarioDto(username, roles, rutas);

                return (ServiceStatus.Ok, authenticatedUser, "Succeeded");

            }
            catch (Exception ex)
            {
                return (ServiceStatus.FailedValidation, null, $"Error Interno de la aplicación... {ex.Message}");
            }
        }


        public async Task<(ServiceStatus, object?, string)> GetAllUsers()
        {

            try
            {

                var usersFromDb = await _context.Users.AsNoTracking().Select(q => new
                {
                    usuario = q.UserName,
                }).ToListAsync();

                var usersWithIndex = usersFromDb.Select((q, index) => new
                {
                    index = index + 1,
                    usuario = q.usuario,
                }).ToList();

                usersWithIndex.Add(new { index = 0, usuario = "TODOS" });

                var ordered = usersWithIndex.OrderBy(q => q.index);

                return (ServiceStatus.Ok, ordered, "Succeeded");

            }
            catch (Exception ex)
            {
                return (ServiceStatus.FailedValidation, null, $"Error Interno de la aplicación... {ex.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarUsuarios()
        {

            try
            {
                var claim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == "username");

                if (claim is null)
                    return (ServiceStatus.FailedValidation, null, "Tenant Invalido");

                var appUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.NormalizedUserName == claim.Value);

                var usuarios = await _context.Users.AsNoTracking()
                                                   .Include(u => u.Sucursal)
                                                   .Where(u => appUser != null && u.TenantId == appUser.TenantId)
                                                   .OrderBy(u => u.UserName)
                                                   .Select(q => new
                                                   {
                                                       id = q.Id,
                                                       usuario = q.UserName,
                                                       nombres = q.FirstName,
                                                       apellidos = q.LastName,
                                                       email = q.Email ?? "",
                                                       telefono = q.PhoneNumber ?? "",
                                                       celular = q.Celular ?? "",
                                                       sucursalId = q.SucursalId,
                                                       sucursal = q.Sucursal != null ? q.Sucursal.Nombre : null,
                                                       estado = q.Estado,
                                                       fechaCreacion = q.FechaCreacion,
                                                       // Sin rol asignado = administrador (acceso completo, como se le da a todo
                                                       // usuario creado desde cero); con rol = colaborador de acceso restringido.
                                                       roles = _context.UserRoles.Where(ur => ur.UserId == q.Id)
                                                                       .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                                                                       .ToList()
                                                   }).ToListAsync();

                return (ServiceStatus.Ok, usuarios, "Success");

            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno de la aplicación... {ex.Message}");
            }
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoUsuario(string id, bool activo)
        {
            try
            {
                var claim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == "username");

                if (claim is null)
                    return (ServiceStatus.FailedValidation, "Tenant Invalido");

                var appUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.NormalizedUserName == claim.Value);

                if (appUser != null && appUser.Id == id && !activo)
                    return (ServiceStatus.FailedValidation, "No puedes desactivar tu propia cuenta");

                var usuario = await _context.Users.AsTracking()
                    .FirstOrDefaultAsync(u => u.Id == id && appUser != null && u.TenantId == appUser.TenantId);

                if (usuario == null)
                    return (ServiceStatus.NotFound, "No se encontro el usuario");

                usuario.Estado = activo;
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, activo ? "Usuario activado" : "Usuario desactivado");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, $"Error Interno de la aplicación... {ex.Message}");
            }
        }

        private async Task<User> FindByUsername(string username)
        {

            var appUser = await _context.Users.AsNoTracking()
                                              .Include(z => z.UserSubmodules)
                                              .ThenInclude(z => z.Submodule)
                                              .ThenInclude(z => z.Module)
                                                             .FirstOrDefaultAsync(x => x.UserName == username.ToUpper());



            if (appUser != null) return appUser;

            return null;

        }
    }
}
