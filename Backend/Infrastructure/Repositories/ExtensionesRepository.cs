using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using System.Security.Claims;

namespace Infrastructure.Repositories
{
    public class ExtensionesRepository : IExtensionesRepository
    {
        private readonly SpaContext _context;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ExtensionesRepository(
            SpaContext context,
            IHttpContextAccessor? httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(ServiceStatus, object?, string)> ListarTipoDocumento()
        {
            try
            {
                var TipoDocumento = await _context.TipoDocumento.AsNoTracking()
                                                          .Select(p => new
                                                          {
                                                              id = p.Id,
                                                              value = p.Nombre
                                                          }).ToListAsync();

                return (ServiceStatus.Ok, TipoDocumento, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }

        }

        public async Task<(ServiceStatus, object?, string)> ListarTipoDocumentoVenta()
        {
            try
            {
                var TipoDocumentoVenta = await _context.TipoDocumentoVenta.AsNoTracking()
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             value = p.Nombre
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, TipoDocumentoVenta, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

    
        public async Task<(ServiceStatus, object?, string)> ListarMetodoPago()
        {
            try
            {
                var Metodopago = await _context.Metodopago.AsNoTracking()
                                                         .Where(p => p.Estado)
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             value = p.Nombre
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, Metodopago, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarMetodoPagoAdmin()
        {
            try
            {
                var metodoPago = await _context.Metodopago.AsNoTracking()
                                                         .OrderBy(p => p.Nombre)
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             value = p.Nombre,
                                                             estado = p.Estado
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, metodoPago, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearMetodoPago(CreateMetodoPagoPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Nombre))
                return (ServiceStatus.FailedValidation, null, "El nombre del método de pago es obligatorio");

            try
            {
                var metodoPago = new Domain.Entities.Metodopago { Nombre = payload.Nombre.Trim() };

                await _context.Metodopago.AddAsync(metodoPago);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, metodoPago, "Método de pago registrado correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.FailedValidation, null, $"Error al registrar método de pago -> {e.InnerException?.Message ?? e.Message}");
            }
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoMetodoPago(int id, bool estado)
        {
            var metodoPago = await _context.Metodopago.AsTracking().FirstOrDefaultAsync(p => p.Id == id);

            if (metodoPago == null)
                return (ServiceStatus.NotFound, $"No se encontró el método de pago {id}");

            metodoPago.Estado = estado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Success");
        }

        public async Task<(ServiceStatus, object?, string)> ListarNacionalidad()
        {
            try
            {
                var Nacionalidad = await _context.Nacionalidad.AsNoTracking()
                                                          .Select(p => new
                                                          {
                                                              id = p.Id,
                                                              value = p.Descripcion
                                                          }).ToListAsync();

                return (ServiceStatus.Ok, Nacionalidad, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }

        }

        public async Task<(ServiceStatus, object?, string)> ListarRubros()
        {
            try
            {
                var rubros = await _context.Rubro.AsNoTracking()
                                                 .Select(p => new
                                                 {
                                                     id = p.Id,
                                                     value = p.Nombre
                                                 }).ToListAsync();

                return (ServiceStatus.Ok, rubros, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarSucursales()
        {
            try
            {
                var sucursales = await _context.Sucursal.AsNoTracking()
                                                        .Select(p => new
                                                        {
                                                            id = p.Id,
                                                            value = p.Nombre,
                                                            direccion = p.Direccion,
                                                            monedaId = p.MonedaId,
                                                            paisId = p.PaisId,
                                                            rubroId = p.RubroId
                                                        }).ToListAsync();

                return (ServiceStatus.Ok, sucursales, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarUbigeos()
        {
            try
            {
                var ubigeos = await _context.Ubigeo.AsNoTracking()
                                                   .OrderBy(u => u.UbigeoId)
                                                   .Select(p => new
                                                   {
                                                       ubigeoId = p.UbigeoId,
                                                       departamento = p.Departamento,
                                                       provincia = p.Provincia,
                                                       distrito = p.Distrito
                                                   }).ToListAsync();

                return (ServiceStatus.Ok, ubigeos, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearSucursal(CreateSucursalPayload payload)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(payload.Nombre))
                    return (ServiceStatus.FailedValidation, null, "Nombre es obligatorio");

                Domain.Entities.Tenant? tenant = null;

                if (!string.IsNullOrWhiteSpace(payload.TenantId))
                {
                    tenant = await _context.Tenant.AsNoTracking()
                                                   .IgnoreQueryFilters()
                                                   .FirstOrDefaultAsync(t =>
                                                       t.TenantKey == payload.TenantId ||
                                                       t.Name == payload.TenantId);
                }
                else
                {
                    var claim = _httpContextAccessor?.HttpContext?.User?.Claims
                        .FirstOrDefault(e => e.Type == "username");

                    if (claim != null)
                    {
                        var appUser = await _context.Users.AsNoTracking()
                                                          .FirstOrDefaultAsync(x => x.UserName == claim.Value);

                        if (appUser != null)
                            tenant = await _context.Tenant.AsNoTracking()
                                                          .IgnoreQueryFilters()
                                                          .FirstOrDefaultAsync(t => t.Identificador == appUser.TenantId);
                    }
                }

                if (tenant is null)
                    return (ServiceStatus.FailedValidation, null, "Tenant Invalido");

                var currentUsername = _httpContextAccessor?.HttpContext?.User?.Claims
                    .FirstOrDefault(e => e.Type == "username")?.Value;

                var sucursal = new Domain.Entities.Sucursal
                {
                    Nombre = payload.Nombre,
                    Direccion = payload.Direccion,
                    UbigeoId = payload.UbigeoId,
                    Latitud = payload.Latitud,
                    Longitud = payload.Longitud,
                    MonedaId = payload.MonedaId > 0 ? payload.MonedaId : tenant?.MonedaId ?? 1,
                    PaisId = payload.PaisId > 0 ? payload.PaisId : tenant?.PaisId ?? 1,
                    RubroId = payload.RubroId > 0 ? payload.RubroId : tenant?.RubroId ?? 1,
                    // El tenant resuelto (por TenantKey o Name), no el string crudo del payload:
                    // así queda consistente con el formato que usan todos los HasQueryFilter (Tenant.Name).
                    TenantId = tenant.Name,
                    UsuarioCreacion = currentUsername,
                    FechaCreacion = DateTime.UtcNow.AddHours(-5)
                };

                await _context.Sucursal.AddAsync(sucursal);
                // SaveChangesRegularAsync (no el override) para no pisar el TenantId de arriba con
                // el tenant ambiente de quien hace la llamada (ej. el SuperAdmin creando otra empresa).
                await _context.SaveChangesRegularAsync();

                return (ServiceStatus.Ok, sucursal, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, string)> ReasignarTenantSucursal(int sucursalId, string tenantKey)
        {
            try
            {
                var sucursal = await _context.Sucursal.IgnoreQueryFilters()
                                                       .FirstOrDefaultAsync(s => s.Id == sucursalId);

                if (sucursal is null)
                    return (ServiceStatus.NotFound, "No existe la sucursal");

                var tenant = await _context.Tenant.AsNoTracking()
                                                   .IgnoreQueryFilters()
                                                   .FirstOrDefaultAsync(t => t.TenantKey == tenantKey || t.Name == tenantKey);

                if (tenant is null)
                    return (ServiceStatus.NotFound, "No existe el tenant");

                sucursal.TenantId = tenant.Name;

                await _context.SaveChangesRegularAsync();

                return (ServiceStatus.Ok, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }
    }
}
