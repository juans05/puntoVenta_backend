using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Application.Interfaces.IRepository;
using Application.Services.BaseService;
using Domain.Entities;
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
        private readonly IBaseService _baseService;
        private readonly PeruApiOptions _peruApiOptions;
        private readonly DniApiOptions _dniApiOptions;

        public ExtensionesRepository(
            SpaContext context,
            IHttpContextAccessor? httpContextAccessor,
            IBaseService baseService,
            IOptions<PeruApiOptions> peruApiOptions,
            IOptions<DniApiOptions> dniApiOptions)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _baseService = baseService;
            _peruApiOptions = peruApiOptions.Value;
            _dniApiOptions = dniApiOptions.Value;
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


        public async Task<(ServiceStatus, object?, string)> ListarMotivosNota(int tipoDocumentoVentaId)
        {
            try
            {
                var MotivoNota = await _context.MotivoNota.AsNoTracking()
                                                         .Where(m => m.TipoDocumentoVentaId == tipoDocumentoVentaId && m.Estado)
                                                         .OrderBy(m => m.Codigo)
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             codigo = p.Codigo,
                                                             value = p.Descripcion,
                                                             revierteStock = p.RevierteStock
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, MotivoNota, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarTiposIgv()
        {
            try
            {
                var TipoIgv = await _context.TipoIgv.AsNoTracking()
                                                         .Where(t => t.Estado)
                                                         .OrderBy(t => t.Codigo)
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             codigo = p.Codigo,
                                                             value = p.Descripcion,
                                                             aplicaPorcentajeImpuesto = p.AplicaPorcentajeImpuesto
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, TipoIgv, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarUnidadesMedida()
        {
            try
            {
                var UnidadMedida = await _context.UnidadMedida.AsNoTracking()
                                                         .Where(u => u.Estado)
                                                         .OrderBy(u => u.Id)
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             codigo = p.Codigo,
                                                             value = p.Descripcion
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, UnidadMedida, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarTiposOperacion()
        {
            try
            {
                var TipoOperacion = await _context.TipoOperacion.AsNoTracking()
                                                         .Where(t => t.Estado)
                                                         .OrderBy(t => t.Codigo)
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             codigo = p.Codigo,
                                                             value = p.Descripcion
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, TipoOperacion, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarMonedas()
        {
            try
            {
                var Moneda = await _context.Moneda.AsNoTracking()
                                                         .Where(m => m.Estado)
                                                         .OrderBy(m => m.Id)
                                                         .Select(p => new
                                                         {
                                                             id = p.Id,
                                                             codigo = p.Codigo,
                                                             simbolo = p.Simbolo,
                                                             value = p.Codigo + " - " + p.Simbolo
                                                         }).ToListAsync();

                return (ServiceStatus.Ok, Moneda, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        // ---- CRUD TipoIgv ----

        public async Task<(ServiceStatus, object?, string)> ListarTiposIgvAdmin()
        {
            try
            {
                var data = await _context.TipoIgv.AsNoTracking()
                                                  .OrderBy(t => t.Codigo)
                                                  .Select(p => new
                                                  {
                                                      id = p.Id,
                                                      codigo = p.Codigo,
                                                      descripcion = p.Descripcion,
                                                      aplicaPorcentajeImpuesto = p.AplicaPorcentajeImpuesto,
                                                      estado = p.Estado,
                                                      esPersonalizado = p.TenantId != null
                                                  }).ToListAsync();

                return (ServiceStatus.Ok, data, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearTipoIgv(CreateTipoIgvPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            try
            {
                var entity = new TipoIgv
                {
                    Codigo = payload.Codigo.Trim(),
                    Descripcion = payload.Descripcion.Trim(),
                    AplicaPorcentajeImpuesto = payload.AplicaPorcentajeImpuesto
                };

                await _context.TipoIgv.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, entity, "Tipo de IGV registrado correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.FailedValidation, null, $"Error al registrar -> {e.InnerException?.Message ?? e.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ActualizarTipoIgv(int id, UpdateTipoIgvPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            var entity = await _context.TipoIgv.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, null, $"No se encontró el tipo de IGV {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, null, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Codigo = payload.Codigo.Trim();
            entity.Descripcion = payload.Descripcion.Trim();
            entity.AplicaPorcentajeImpuesto = payload.AplicaPorcentajeImpuesto;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, entity, "Actualizado correctamente");
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoTipoIgv(int id, bool estado)
        {
            var entity = await _context.TipoIgv.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, $"No se encontró el tipo de IGV {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Estado = estado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Success");
        }

        // ---- CRUD UnidadMedida ----

        public async Task<(ServiceStatus, object?, string)> ListarUnidadesMedidaAdmin()
        {
            try
            {
                var data = await _context.UnidadMedida.AsNoTracking()
                                                       .OrderBy(u => u.Codigo)
                                                       .Select(p => new
                                                       {
                                                           id = p.Id,
                                                           codigo = p.Codigo,
                                                           descripcion = p.Descripcion,
                                                           estado = p.Estado,
                                                           esPersonalizado = p.TenantId != null
                                                       }).ToListAsync();

                return (ServiceStatus.Ok, data, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearUnidadMedida(CreateUnidadMedidaPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            try
            {
                var entity = new UnidadMedida { Codigo = payload.Codigo.Trim(), Descripcion = payload.Descripcion.Trim() };

                await _context.UnidadMedida.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, entity, "Unidad de medida registrada correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.FailedValidation, null, $"Error al registrar -> {e.InnerException?.Message ?? e.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ActualizarUnidadMedida(int id, UpdateUnidadMedidaPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            var entity = await _context.UnidadMedida.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, null, $"No se encontró la unidad de medida {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, null, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Codigo = payload.Codigo.Trim();
            entity.Descripcion = payload.Descripcion.Trim();
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, entity, "Actualizado correctamente");
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoUnidadMedida(int id, bool estado)
        {
            var entity = await _context.UnidadMedida.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, $"No se encontró la unidad de medida {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Estado = estado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Success");
        }

        // ---- CRUD TipoOperacion ----

        public async Task<(ServiceStatus, object?, string)> ListarTiposOperacionAdmin()
        {
            try
            {
                var data = await _context.TipoOperacion.AsNoTracking()
                                                        .OrderBy(t => t.Codigo)
                                                        .Select(p => new
                                                        {
                                                            id = p.Id,
                                                            codigo = p.Codigo,
                                                            descripcion = p.Descripcion,
                                                            estado = p.Estado,
                                                            esPersonalizado = p.TenantId != null
                                                        }).ToListAsync();

                return (ServiceStatus.Ok, data, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearTipoOperacion(CreateTipoOperacionPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            try
            {
                var entity = new TipoOperacion { Codigo = payload.Codigo.Trim(), Descripcion = payload.Descripcion.Trim() };

                await _context.TipoOperacion.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, entity, "Tipo de operación registrado correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.FailedValidation, null, $"Error al registrar -> {e.InnerException?.Message ?? e.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ActualizarTipoOperacion(int id, UpdateTipoOperacionPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            var entity = await _context.TipoOperacion.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, null, $"No se encontró el tipo de operación {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, null, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Codigo = payload.Codigo.Trim();
            entity.Descripcion = payload.Descripcion.Trim();
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, entity, "Actualizado correctamente");
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoTipoOperacion(int id, bool estado)
        {
            var entity = await _context.TipoOperacion.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, $"No se encontró el tipo de operación {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Estado = estado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Success");
        }

        // ---- CRUD MotivoNota ----

        public async Task<(ServiceStatus, object?, string)> ListarMotivosNotaAdmin()
        {
            try
            {
                var data = await _context.MotivoNota.AsNoTracking()
                                                     .OrderBy(m => m.Codigo)
                                                     .Select(p => new
                                                     {
                                                         id = p.Id,
                                                         codigo = p.Codigo,
                                                         descripcion = p.Descripcion,
                                                         revierteStock = p.RevierteStock,
                                                         tipoDocumentoVentaId = p.TipoDocumentoVentaId,
                                                         estado = p.Estado,
                                                         esPersonalizado = p.TenantId != null
                                                     }).ToListAsync();

                return (ServiceStatus.Ok, data, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearMotivoNota(CreateMotivoNotaPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            try
            {
                var entity = new MotivoNota
                {
                    Codigo = payload.Codigo.Trim(),
                    Descripcion = payload.Descripcion.Trim(),
                    RevierteStock = payload.RevierteStock,
                    TipoDocumentoVentaId = payload.TipoDocumentoVentaId
                };

                await _context.MotivoNota.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, entity, "Motivo de nota registrado correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.FailedValidation, null, $"Error al registrar -> {e.InnerException?.Message ?? e.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ActualizarMotivoNota(int id, UpdateMotivoNotaPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Descripcion))
                return (ServiceStatus.FailedValidation, null, "Código y descripción son obligatorios");

            var entity = await _context.MotivoNota.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, null, $"No se encontró el motivo de nota {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, null, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Codigo = payload.Codigo.Trim();
            entity.Descripcion = payload.Descripcion.Trim();
            entity.RevierteStock = payload.RevierteStock;
            entity.TipoDocumentoVentaId = payload.TipoDocumentoVentaId;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, entity, "Actualizado correctamente");
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoMotivoNota(int id, bool estado)
        {
            var entity = await _context.MotivoNota.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, $"No se encontró el motivo de nota {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Estado = estado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Success");
        }

        // ---- CRUD Moneda ----

        public async Task<(ServiceStatus, object?, string)> ListarMonedasAdmin()
        {
            try
            {
                var data = await _context.Moneda.AsNoTracking()
                                                 .OrderBy(m => m.Codigo)
                                                 .Select(p => new
                                                 {
                                                     id = p.Id,
                                                     codigo = p.Codigo,
                                                     simbolo = p.Simbolo,
                                                     locale = p.Locale,
                                                     paisId = p.PaisId,
                                                     estado = p.Estado,
                                                     esPersonalizado = p.TenantId != null
                                                 }).ToListAsync();

                return (ServiceStatus.Ok, data, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearMoneda(CreateMonedaPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Simbolo) || string.IsNullOrWhiteSpace(payload.Locale))
                return (ServiceStatus.FailedValidation, null, "Código, símbolo y locale son obligatorios");

            try
            {
                var entity = new Moneda
                {
                    Codigo = payload.Codigo.Trim(),
                    Simbolo = payload.Simbolo.Trim(),
                    Locale = payload.Locale.Trim(),
                    PaisId = payload.PaisId
                };

                await _context.Moneda.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, entity, "Moneda registrada correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.FailedValidation, null, $"Error al registrar -> {e.InnerException?.Message ?? e.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ActualizarMoneda(int id, UpdateMonedaPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Codigo) || string.IsNullOrWhiteSpace(payload.Simbolo) || string.IsNullOrWhiteSpace(payload.Locale))
                return (ServiceStatus.FailedValidation, null, "Código, símbolo y locale son obligatorios");

            var entity = await _context.Moneda.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, null, $"No se encontró la moneda {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, null, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Codigo = payload.Codigo.Trim();
            entity.Simbolo = payload.Simbolo.Trim();
            entity.Locale = payload.Locale.Trim();
            entity.PaisId = payload.PaisId;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, entity, "Actualizado correctamente");
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoMoneda(int id, bool estado)
        {
            var entity = await _context.Moneda.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return (ServiceStatus.NotFound, $"No se encontró la moneda {id}");
            if (entity.TenantId == null)
                return (ServiceStatus.FailedValidation, "No se puede modificar un catálogo base de SUNAT, solo tus propios registros");

            entity.Estado = estado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Success");
        }

        // Consulta el RUC en peruapi.com -- la llave se queda en el backend (appsettings/env var)
        // para que nunca se exponga en el bundle del frontend.
        public async Task<(ServiceStatus, object?, string)> ConsultarRuc(string ruc)
        {
            try
            {
                var resultado = await _baseService.SendAsync<PeruApiRucResponse>(new ApiRequest
                {
                    apiType = SD.ApiType.GET,
                    Url = $"{_peruApiOptions.BaseUrl}/ruc/{ruc}?plan=true",
                    ApiKey = _peruApiOptions.ApiKey
                });

                if (resultado is null || resultado.Code != "200")
                    return (ServiceStatus.FailedValidation, null, "No se encontró el RUC consultado");

                var data = new
                {
                    ruc = resultado.Ruc,
                    razonSocial = resultado.RazonSocial,
                    direccion = resultado.Direccion,
                    ubigeoId = resultado.Ubigeo,
                    estado = resultado.Estado,
                    condicion = resultado.Condicion
                };

                return (ServiceStatus.Ok, data, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ConsultarDni(string dni)
        {
            try
            {
                var resultado = await _baseService.SendAsync<DniLookupResponse>(new ApiRequest
                {
                    apiType = SD.ApiType.GET,
                    Url = $"{_dniApiOptions.BaseUrl}?documento={dni}"
                });

                if (resultado is null || string.IsNullOrWhiteSpace(resultado.NumeroDocumento))
                    return (ServiceStatus.FailedValidation, null, "No se encontró el DNI consultado");

                var nombreCompleto = string.Join(" ", new[] { resultado.ApellidoPaterno, resultado.ApellidoMaterno, resultado.Nombre }
                    .Where(p => !string.IsNullOrWhiteSpace(p)));

                var data = new
                {
                    numeroDocumento = resultado.NumeroDocumento,
                    razonSocial = nombreCompleto,
                    nombres = resultado.Nombre,
                    apellidoPaterno = resultado.ApellidoPaterno,
                    apellidoMaterno = resultado.ApellidoMaterno,
                    celular = resultado.TelefonoMovil,
                    fechaNacimiento = resultado.FechaNacimiento
                };

                return (ServiceStatus.Ok, data, "Success");
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

        public async Task<(ServiceStatus, object?, string)> ListarPaises()
        {
            try
            {
                var paises = await _context.Pais.AsNoTracking()
                                                 .OrderBy(p => p.Nombre)
                                                 .Select(p => new
                                                 {
                                                     id = p.Id,
                                                     codigo = p.Codigo,
                                                     value = p.Nombre
                                                 }).ToListAsync();

                return (ServiceStatus.Ok, paises, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
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
                                                            ubigeoId = p.UbigeoId,
                                                            monedaId = p.MonedaId,
                                                            paisId = p.PaisId,
                                                            rubroId = p.RubroId,
                                                            serieFactura = p.SerieFactura,
                                                            serieBoleta = p.SerieBoleta,
                                                            codigoEstablecimiento = p.CodigoEstablecimiento,
                                                            urbanizacion = p.Urbanizacion,
                                                            telefono = p.Telefono,
                                                            correo = p.Correo
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

        public async Task<(ServiceStatus, object?, string)> ListarSalones(string ubigeoId)
        {
            try
            {
                var salones = await _context.Salon.AsNoTracking()
                                                   .Where(s => s.Activo && s.UbigeoId == ubigeoId)
                                                   .OrderBy(s => s.Nombre)
                                                   .Select(s => new
                                                   {
                                                       id = s.Id,
                                                       nombre = s.Nombre
                                                   }).ToListAsync();

                return (ServiceStatus.Ok, salones, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ListarSalonesAdmin()
        {
            try
            {
                var salones = await _context.Salon.AsNoTracking()
                                                   .Include(s => s.Ubigeo)
                                                   .OrderBy(s => s.Nombre)
                                                   .Select(s => new
                                                   {
                                                       id = s.Id,
                                                       nombre = s.Nombre,
                                                       ubigeoId = s.UbigeoId,
                                                       ubigeoNombre = s.Ubigeo != null ? $"{s.Ubigeo.Departamento} - {s.Ubigeo.Provincia} - {s.Ubigeo.Distrito}" : null,
                                                       estado = s.Activo
                                                   }).ToListAsync();

                return (ServiceStatus.Ok, salones, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearSalon(CreateSalonPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Nombre))
                return (ServiceStatus.FailedValidation, null, "El nombre del salón es obligatorio");

            if (string.IsNullOrWhiteSpace(payload.UbigeoId))
                return (ServiceStatus.FailedValidation, null, "Debes elegir la ciudad del salón");

            try
            {
                var ubigeoExiste = await _context.Ubigeo.AnyAsync(u => u.UbigeoId == payload.UbigeoId);
                if (!ubigeoExiste)
                    return (ServiceStatus.FailedValidation, null, "La ciudad elegida no es válida");

                var salon = new Domain.Entities.Salon { Nombre = payload.Nombre.Trim(), UbigeoId = payload.UbigeoId, Activo = true };

                await _context.Salon.AddAsync(salon);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, salon, "Salón registrado correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.FailedValidation, null, $"Error al registrar salón -> {e.InnerException?.Message ?? e.Message}");
            }
        }

        public async Task<(ServiceStatus, string)> CambiarEstadoSalon(int id, bool estado)
        {
            var salon = await _context.Salon.AsTracking().FirstOrDefaultAsync(s => s.Id == id);

            if (salon == null)
                return (ServiceStatus.NotFound, $"No se encontró el salón {id}");

            salon.Activo = estado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Success");
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
                                                          .FirstOrDefaultAsync(x => x.NormalizedUserName == claim.Value);

                        if (appUser != null)
                            tenant = await _context.Tenant.AsNoTracking()
                                                          .IgnoreQueryFilters()
                                                          .FirstOrDefaultAsync(t => t.Identificador == appUser.TenantId);
                    }
                }

                if (tenant is null)
                    return (ServiceStatus.FailedValidation, null, "Tenant Invalido");

                var serieFactura = string.IsNullOrWhiteSpace(payload.SerieFactura) ? null : payload.SerieFactura.Trim();
                var serieBoleta = string.IsNullOrWhiteSpace(payload.SerieBoleta) ? null : payload.SerieBoleta.Trim();

                // SUNAT rechaza dos locales con la misma serie -- se valida antes de crear nada.
                if (serieFactura != null && await _context.Sucursal.AnyAsync(s => s.TenantId == tenant.Name && s.SerieFactura == serieFactura))
                    return (ServiceStatus.FailedValidation, null, $"La serie {serieFactura} ya la usa otra sucursal");

                if (serieBoleta != null && await _context.Sucursal.AnyAsync(s => s.TenantId == tenant.Name && s.SerieBoleta == serieBoleta))
                    return (ServiceStatus.FailedValidation, null, $"La serie {serieBoleta} ya la usa otra sucursal");

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
                    // 604 = Peru (ver Default/pais.json) -- el Id de Pais no es correlativo desde 1
                    // como los demas catalogos, asi que el fallback anterior (1) no existia y violaba la FK.
                    PaisId = payload.PaisId > 0 ? payload.PaisId : (tenant != null && tenant.PaisId > 0 ? tenant.PaisId.Value : 604),
                    RubroId = payload.RubroId > 0 ? payload.RubroId : tenant?.RubroId ?? 1,
                    SerieFactura = serieFactura,
                    SerieBoleta = serieBoleta,
                    CodigoEstablecimiento = string.IsNullOrWhiteSpace(payload.CodigoEstablecimiento) ? null : payload.CodigoEstablecimiento.Trim(),
                    Urbanizacion = string.IsNullOrWhiteSpace(payload.Urbanizacion) ? null : payload.Urbanizacion.Trim(),
                    Telefono = string.IsNullOrWhiteSpace(payload.Telefono) ? null : payload.Telefono.Trim(),
                    Correo = string.IsNullOrWhiteSpace(payload.Correo) ? null : payload.Correo.Trim(),
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

                // "Empieza en" no es el correlativo: es el primer numero que va a llevar el
                // proximo comprobante de esa serie. CrearComprobante incrementa antes de usar, asi
                // que el contador arranca en (empiezaEn - 1).
                if (serieFactura != null)
                    _context.Seriecorrelativo.Add(new Domain.Entities.Seriecorrelativo
                    {
                        SucursalId = sucursal.Id,
                        TipoDocumentoVentaId = 1,
                        Serie = serieFactura,
                        Correlativo = Math.Max(0, (payload.EmpiezaEnFactura ?? 1) - 1),
                        TenantId = tenant.Name,
                        UsuarioCreacion = currentUsername,
                        FechaCreacion = DateTime.UtcNow.AddHours(-5)
                    });

                if (serieBoleta != null)
                    _context.Seriecorrelativo.Add(new Domain.Entities.Seriecorrelativo
                    {
                        SucursalId = sucursal.Id,
                        TipoDocumentoVentaId = 2,
                        Serie = serieBoleta,
                        Correlativo = Math.Max(0, (payload.EmpiezaEnBoleta ?? 1) - 1),
                        TenantId = tenant.Name,
                        UsuarioCreacion = currentUsername,
                        FechaCreacion = DateTime.UtcNow.AddHours(-5)
                    });

                if (serieFactura != null || serieBoleta != null)
                    await _context.SaveChangesRegularAsync();

                return (ServiceStatus.Ok, sucursal, "Success");
            }
            catch (Exception e)
            {
                // InnerException primero: e.Message casi nunca es null (es el mensaje generico de
                // EF "See the inner exception for details"), asi que el orden inverso ocultaba
                // siempre la causa real (ej. violacion de FK) detras de ese texto generico.
                return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
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
