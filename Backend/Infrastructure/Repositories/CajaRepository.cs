using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Enumerations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.IRepository;
using Application.Helper;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using System.Security.Claims;
using Domain.Entities;
using Domain.Tenant;

namespace Infrastructure.Repositories
{
    public class CajaRepository : ICajaRepository
    {
        private readonly SpaContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor? _httpContextAccessor;


        public CajaRepository(
            SpaContext context,
            IMapper mapper,
            IHttpContextAccessor? httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        private int? PaisIdClaim =>
            _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
                && int.TryParse(claim, out var pais) ? pais : (int?)null;

        private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

        private List<MetodoPagoMontoDto> AgruparPorMetodo(List<Pago> pagos) =>
            pagos.GroupBy(p => new { p.MetodoPagoId, Nombre = p.Metodopago != null ? p.Metodopago.Descripcion ?? p.Metodopago.Nombre : null })
                 .Select(g => new MetodoPagoMontoDto
                 {
                     MetodoPagoId = g.Key.MetodoPagoId,
                     Nombre = g.Key.Nombre,
                     Monto = g.Sum(p => p.Monto)
                 })
                 .ToList();


        public async Task<(ServiceStatus, object?, string)> MontoActual(string usuario, int? sucursalId = null)
        {

            var userUpper = usuario.ToUpper();
            sucursalId ??= _context.CurrentSucursalId;

            var timenow = NowLocal();

            try
            {
                var existeRegistro = await _context.Caja.Include(x => x.Retiros)
                                                        .AsNoTracking()
                                                        .Where(x => x.UsuarioCreacion == userUpper && x.FechaCreacion.Date == timenow.Date && x.FechaHoraCierre == null && x.SucursalId == sucursalId)
                                                        .FirstOrDefaultAsync();

                if (existeRegistro == null)
                {
                    return (ServiceStatus.NotFound, 0, $"No existe registro de caja para el usuario {usuario}");
                }
                else
                {
                    List<Pago> pagos = new List<Pago>();

                    pagos = await _context.Pago.AsNoTracking().Include(x => x.Metodopago).Where(x => x.UsuarioCreacion == userUpper &&
                                                                          x.FechaCreacion.Date == timenow.Date &&
                                                                          x.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado &&
                                                                          x.CajaId == null).ToListAsync();

                    var montoEfec = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Efectivo).Sum(x => x.Monto);
                    var montoTar = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Tarjeta).Sum(x => x.Monto);
                    var montoYap = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Yape).Sum(x => x.Monto);

                    var retiros = existeRegistro.Retiros.ToList();

                    var retirosMap = _mapper.Map<List<RetiroDto>>(retiros);

                    return (ServiceStatus.Ok, new PagoDto
                    {
                        CajaId = existeRegistro.Id,
                        CajaAbierta = existeRegistro == null ? false : true,
                        MontoInicio = existeRegistro.MontoInicio,
                        MontoCierre = existeRegistro.MontoCierre,
                        MontoEfectivo = montoEfec,
                        MontoTarjeta = montoTar,
                        MontoYape = montoYap,
                        MontosPorMetodo = AgruparPorMetodo(pagos),
                        Retiros = retirosMap
                    }, $"Monto actual de Caja");
                }

            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> AbrirCaja(string monto, int? sucursalId = null)
        {

            var user = _httpContextAccessor!.HttpContext!.User.FindFirstValue("username").ToUpper();
            sucursalId ??= _context.CurrentSucursalId;
            var timenow = NowLocal();


            if (string.IsNullOrEmpty(user))
                return (ServiceStatus.FailedValidation, null, $"Error -> Usuario invalido");

            try
            {

                var primerRegistro = await _context.Caja.AsNoTracking()
                                                        .Where(x => x.UsuarioCreacion == user && x.FechaCreacion.Date == timenow.Date && x.FechaHoraCierre == null && x.SucursalId == sucursalId)
                                                        .ToListAsync();

                if (primerRegistro.Count == 1)
                    return (ServiceStatus.FailedValidation, null, $"Error -> Antes de abrir una caja cierre la otra");

                var caja = await _context.Caja.AddAsync(new Caja
                {
                    MontoInicio = decimal.Parse(monto),
                    MontoCierre = 0,
                    SucursalId = sucursalId,
                });

                await _context.SaveChangesAsync();


                return (ServiceStatus.Ok, new PagoDto
                {
                    MontoInicio = decimal.Parse(monto),
                    CajaAbierta = true,
                    CajaId = caja.Entity.Id
                }, "Caja abierta correctamente");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        public async Task<(ServiceStatus, object?, string)> ListarCajas()
        {
            try
            {
                var cajas = await _context.CajaFisica.IgnoreQueryFilters()
                                                     .AsNoTracking()
                                                     .Where(c => c.TenantId == _context.CurrentTenantName)
                                                     .Include(c => c.Sucursal)
                                                     .OrderBy(c => c.Nombre)
                                                     .Select(c => new
                                                     {
                                                         id = c.Id,
                                                         nombre = c.Nombre,
                                                         sucursalId = c.SucursalId,
                                                         sucursal = c.Sucursal != null ? c.Sucursal.Nombre : null
                                                     }).ToListAsync();

                return (ServiceStatus.Ok, cajas, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearCaja(CreateCajaPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Nombre))
                return (ServiceStatus.FailedValidation, null, "El nombre de la caja es obligatorio");

            try
            {
                var caja = new CajaFisica
                {
                    Nombre = payload.Nombre.Trim(),
                    SucursalId = payload.SucursalId
                };

                await _context.CajaFisica.AddAsync(caja);
                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, new { id = caja.Id, nombre = caja.Nombre, sucursalId = caja.SucursalId }, "Caja creada correctamente");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.Message ?? e.InnerException?.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> Retiro(CreateRetiroPayload payload)
        {

            var user = _httpContextAccessor!.HttpContext!.User.FindFirstValue("username").ToUpper();
            var timenow = NowLocal();


            if (string.IsNullOrEmpty(user))
                return (ServiceStatus.FailedValidation, null, $"Error -> Usuario invalido");

            try
            {
                var nuevoRetiro = new Retiros
                {
                    CajaId = payload.CajaId,
                    Monto = payload.Monto,
                    Motivo = payload.Motivo
                };

                await _context.Retiros.AddAsync(nuevoRetiro);

                await _context.SaveChangesAsync();

                //*******************************************

                var existeRegistro = await _context.Caja.Include(x => x.Retiros)
                                                       .AsNoTracking()
                                                       .Where(x => x.Id == payload.CajaId)
                                                       .FirstOrDefaultAsync();


                List<Pago> pagos = new List<Pago>();

                pagos = await _context.Pago.AsNoTracking().Include(x => x.Metodopago).Where(x => x.UsuarioCreacion == user &&
                                                                      x.FechaCreacion.Date == timenow.Date &&
                                                                      x.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado &&
                                                                      x.CajaId == null).ToListAsync();

                var montoEfec = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Efectivo).Sum(x => x.Monto);
                var montoTar = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Tarjeta).Sum(x => x.Monto);
                var montoYap = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Yape).Sum(x => x.Monto);

                var retiros = existeRegistro.Retiros.ToList();

                var retirosMap = _mapper.Map<List<RetiroDto>>(retiros);

                return (ServiceStatus.Ok, new PagoDto
                {
                    CajaId = payload.CajaId,
                    CajaAbierta = true,
                    MontoInicio = existeRegistro.MontoInicio,
                    MontoCierre = existeRegistro.MontoCierre,
                    MontoEfectivo = montoEfec,
                    MontoTarjeta = montoTar,
                    MontoYape = montoYap,
                    MontosPorMetodo = AgruparPorMetodo(pagos),
                    Retiros = retirosMap
                }, $"Registro exitoso");

            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> CerrarCaja(int? sucursalId = null)
        {
            var user = _httpContextAccessor!.HttpContext!.User.FindFirstValue("username").ToUpper();
            sucursalId ??= _context.CurrentSucursalId;

            if (string.IsNullOrEmpty(user))
                return (ServiceStatus.FailedValidation, null, $"Error -> Usuario invalido");

            var timenow = NowLocal();

            try
            {
                var existeRegistro = await _context.Caja.AsTracking()
                                                        .Where(x => x.UsuarioCreacion == user && x.FechaCreacion.Date == timenow.Date && x.FechaHoraCierre == null && x.SucursalId == sucursalId)
                                                        .FirstOrDefaultAsync();

                if (existeRegistro == null)
                {
                    return (ServiceStatus.FailedValidation, null, $"No existe caja abierta para el usuario {user}");
                }
                else
                {

                    var pagos = await _context.Pago.AsTracking().Include(x => x.Metodopago)
                                                   .Where(x => x.UsuarioCreacion == user && x.FechaCreacion.Date == timenow.Date && x.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado && x.CajaId == null)
                                                   .ToListAsync();

                    pagos.ForEach(x => x.CajaId = existeRegistro.Id);

                    existeRegistro.MontoCierre = existeRegistro.MontoInicio + pagos.Sum(p => p.Monto);
                    existeRegistro.FechaHoraCierre = NowLocal();
                    await _context.SaveChangesAsync();

                    var montoEfec = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Efectivo).Sum(x => x.Monto);
                    var montoTar = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Tarjeta).Sum(x => x.Monto);
                    var montoYap = pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Yape).Sum(x => x.Monto);

                    return (ServiceStatus.Ok, new PagoDto
                    {
                        CajaAbierta = false,
                        MontoInicio = existeRegistro.MontoInicio,
                        MontoCierre = existeRegistro.MontoInicio + pagos.Sum(p => p.Monto),
                        MontoEfectivo = montoEfec,
                        MontoTarjeta = montoTar,
                        MontoYape = montoYap,
                        MontosPorMetodo = AgruparPorMetodo(pagos),
                    }, "Caja Cerrada Correctamente");
                }
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }


        public async Task<(ServiceStatus, object?, string)> ReporteCaja(string usuario, string fecha, int? sucursalId = null)
        {
            var usuarioUpper = usuario.ToUpper();
            sucursalId ??= _context.CurrentSucursalId;

            DateTime start = new DateTime();

            if (!string.IsNullOrEmpty(fecha) && !DateTime.TryParse(fecha, out start))

                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {fecha}");


            var timenow = NowLocal();

            try
            {

                List<ComprobanteCabeceraDTO> ventasRealizadas = new();

                if (usuario == "TODOS")
                {
                    ventasRealizadas = await _context.ComprobanteCabecera.AsNoTracking()
                                                                      .Where(x => (x.FechaVenta ?? x.FechaCreacion).Date == start.Date && x.EstadoComprobante != EstatusComprobante.Anulado && (x.SucursalId == null || x.SucursalId == sucursalId))
                                                                      .ProjectTo<ComprobanteCabeceraDTO>(_mapper.ConfigurationProvider)
                                                                      .ToListAsync();
                }
                else
                {
                    ventasRealizadas = await _context.ComprobanteCabecera.AsNoTracking()
                                                                   .Where(x => x.UsuarioCreacion == usuarioUpper && (x.FechaVenta ?? x.FechaCreacion).Date == start.Date && x.EstadoComprobante != EstatusComprobante.Anulado && (x.SucursalId == null || x.SucursalId == sucursalId))
                                                                   .ProjectTo<ComprobanteCabeceraDTO>(_mapper.ConfigurationProvider)
                                                                   .ToListAsync();
                }



                if (ventasRealizadas.Count == 0)
                    return (ServiceStatus.NotFound, null, $"No se encontraron ventas realizadas para el usuario {usuario}");

                return (ServiceStatus.Ok, ventasRealizadas, $"Reporte de ventas realizadas para el usuario {usuario}");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }


        public async Task<(ServiceStatus, object?, string)> HistoricoCierreCajaUsuario(string fecha, int? sucursalId = null)
        {
            var user = _httpContextAccessor!.HttpContext!.User.FindFirstValue("username").ToUpper();
            sucursalId ??= _context.CurrentSucursalId;

            if (string.IsNullOrEmpty(user))
                return (ServiceStatus.FailedValidation, null, $"Error -> Usuario invalido");

            var timenow = NowLocal();

            DateTime start = new DateTime();

            if (!string.IsNullOrEmpty(fecha) && !DateTime.TryParse(fecha, out start))

                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {fecha}");

            try
            {


                var aperturas = await _context.Caja.AsNoTracking()
                                                    .Where(x => x.FechaCreacion.Date == start.Date && x.UsuarioCreacion == user && x.SucursalId == sucursalId)
                                                    .Select(q => new
                                                    {
                                                        sucursalId = q.SucursalId,
                                                        cajaAbierta = q.FechaHoraCierre == null ? true : false,
                                                        montoInicio = q.MontoInicio,
                                                        montoCierre = q.MontoCierre,
                                                        fechaHoraApertura = q.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss"),
                                                        fechaHoraCierre = q.FechaHoraCierre.HasValue ? q.FechaHoraCierre.Value.ToString("dd/MM/yyyy HH:mm:ss") : null,
                                                    })
                                                    .ToListAsync();


                if (aperturas.Count == 0)
                    return (ServiceStatus.NotFound, null, $"No se encontraron cierres de caja para el usuario {user}");

                return (ServiceStatus.Ok, aperturas, $"Cierres de caja para el usuario {user}");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> ReporteCajaResumido(string usuario, string fecha, int? sucursalId = null)
        {
            var usuarioUpper = usuario.ToUpper();
            sucursalId ??= _context.CurrentSucursalId;

            DateTime start = new DateTime();

            if (!string.IsNullOrEmpty(fecha) && !DateTime.TryParse(fecha, out start))

                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {fecha}");


            var timenow = NowLocal();

            List<ComprobanteCabeceraDTO> comprobanteCabeceraDTO = new();

            try
            {
                if (usuario == "TODOS")
                {
                    comprobanteCabeceraDTO = await _context.ComprobanteCabecera.AsNoTracking()
                                                                       .Where(x => (x.FechaVenta ?? x.FechaCreacion).Date == start.Date && x.EstadoComprobante != EstatusComprobante.Anulado && (x.SucursalId == null || x.SucursalId == sucursalId))
                                                                       .ProjectTo<ComprobanteCabeceraDTO>(_mapper.ConfigurationProvider)
                                                                       .ToListAsync();
                }
                else
                {
                    comprobanteCabeceraDTO = await _context.ComprobanteCabecera.AsNoTracking()
                                                                       .Where(x => x.UsuarioCreacion == usuarioUpper && (x.FechaVenta ?? x.FechaCreacion).Date == start.Date && x.EstadoComprobante != EstatusComprobante.Anulado && (x.SucursalId == null || x.SucursalId == sucursalId))
                                                                       .ProjectTo<ComprobanteCabeceraDTO>(_mapper.ConfigurationProvider)
                                                                       .ToListAsync();
                }

                if (comprobanteCabeceraDTO.Count == 0)
                    return (ServiceStatus.NotFound, null, $"No se encontraron ventas realizadas para el usuario {usuario}");

                var objeto = new
                {
                    boletas = new
                    {
                        cantidad = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Boleta).Count(),
                        efectivo = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Boleta).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Efectivo).Sum(x => x.Monto)).Sum(),
                        tarjeta = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Boleta).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Tarjeta).Sum(x => x.Monto)).Sum(),
                        yape = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Boleta).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Yape).Sum(x => x.Monto)).Sum(),
                        total = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Boleta).Select(x => x.Pagos.Sum(x => x.Monto)).Sum(),
                    },
                    facturas = new
                    {
                        cantidad = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Factura).Count(),
                        efectivo = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Factura).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Efectivo).Sum(x => x.Monto)).Sum(),
                        tarjeta = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Factura).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Tarjeta).Sum(x => x.Monto)).Sum(),
                        yape = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Factura).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Yape).Sum(x => x.Monto)).Sum(),
                        total = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.Factura).Select(x => x.Pagos.Sum(x => x.Monto)).Sum(),
                    },
                    ticketInterno = new
                    {
                        cantidad = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.TicketInterno).Count(),
                        efectivo = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.TicketInterno).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Efectivo).Sum(x => x.Monto)).Sum(),
                        tarjeta = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.TicketInterno).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Tarjeta).Sum(x => x.Monto)).Sum(),
                        yape = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.TicketInterno).Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Yape).Sum(x => x.Monto)).Sum(),
                        total = comprobanteCabeceraDTO.Where(x => x.TipoDocumentoVentaId == (int)TipoComprobante.TicketInterno).Select(x => x.Pagos.Sum(x => x.Monto)).Sum(),
                    },
                    total = new
                    {
                        cantidad = comprobanteCabeceraDTO.Count(),
                        efectivo = comprobanteCabeceraDTO.Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Efectivo).Sum(x => x.Monto)).Sum(),
                        tarjeta = comprobanteCabeceraDTO.Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Tarjeta).Sum(x => x.Monto)).Sum(),
                        yape = comprobanteCabeceraDTO.Select(x => x.Pagos.Where(x => x.MetodoPagoId == (int)TipoPago.Yape).Sum(x => x.Monto)).Sum(),
                        total = comprobanteCabeceraDTO.Select(x => x.Pagos.Sum(x => x.Monto)).Sum(),
                    }
                };

                return (ServiceStatus.Ok, objeto, $"Reporte de ventas realizadas para el usuario {usuario}");


            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
