using Application.Interfaces.IRepository;
using Application.Abstractions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.Common.Utils;
using Domain.DTO;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Domain.Tenant;
using Domain.Utils;
using Infrastructure.Common;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories
{
    public class ComprobanteRepository : IComprobanteRepository
    {
        private readonly SpaContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly TaxCalculatorFactory _taxCalculatorFactory;


        public ComprobanteRepository(
            SpaContext context,
            IMapper mapper,
            IHttpContextAccessor? httpContextAccessor,
            TaxCalculatorFactory taxCalculatorFactory)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _taxCalculatorFactory = taxCalculatorFactory;
        }
        public async Task<(ServiceStatus, object?, string)> CrearComprobante(ComprobantePayload payload)
        {
            //crear la cabecera
            await _context.Database.BeginTransactionAsync();

            var serieColletativoCreado = string.Empty;

            try
            {
                var sumatoria = payload.DetalleComprobante.Sum(q => q.ValorUnitario * q.Cantidad);


                if (payload.Total != sumatoria)
                    return (ServiceStatus.FailedValidation, null, "El total no coincide con la suma de los detalles");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Factura && string.IsNullOrEmpty(payload.NumeroDocumento))
                    return (ServiceStatus.FailedValidation, null, "Por favor, ingrese el numero de ruc");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Factura && string.IsNullOrEmpty(payload.RazonSocial))
                    return (ServiceStatus.FailedValidation, null, "Por favor, envie el nombre de la Razon Social");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Factura && payload.NumeroDocumento.Length != 11)
                    return (ServiceStatus.FailedValidation, null, "Por favor, indique un numero de ruc valido");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Boleta && !string.IsNullOrEmpty(payload.NumeroDocumento) && string.IsNullOrEmpty(payload.RazonSocial))
                    return (ServiceStatus.FailedValidation, null, "Por favor, ingrese el nombre");


                var cabecera = _mapper.Map<ComprobanteCabecera>(payload);

                var (_, config) = await ObtenerConfiguracionFiscalPorTenant(_context.CurrentTenantName);

                var paisId = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } paisClaim
                            && int.TryParse(paisClaim, out var parsedPais) ? parsedPais : (int?)null;

                var impuesto = _taxCalculatorFactory.GetCalculator(paisId)
                                                    .GetImpuestoRate(paisId, config?.PorcentajeImpuesto);
                var factor = 1m + (impuesto / 100m);

                cabecera.ValorTotal = payload.Total;

                cabecera.ValorSubtotal = Math.Round(payload.Total / factor, 2);

                cabecera.ValorIgv = payload.Total - Math.Round(payload.Total / factor, 2);

                cabecera.PorcentajeImpuesto = impuesto;

                cabecera.TotalLetras = DecimalExtensions.ConvertirNumeroALetras(payload.Total);

                if (payload.TipoDocumentoVentaId == 1)
                    cabecera.Serie = config?.SerieFactura ?? "FAC";
                else if (payload.TipoDocumentoVentaId == 2)
                    cabecera.Serie = config?.SerieBoleta ?? "BOL";
                else
                    cabecera.Serie = config?.SerieNota ?? "TIN";


                var serieCorrelativo = await _context.Seriecorrelativo.Where(x => x.Serie == cabecera.Serie)
                                                                      .AsTracking()
                                                                      .FirstOrDefaultAsync();


                if (serieCorrelativo == null)
                    return (ServiceStatus.FailedValidation, null, "No se encontro correlativo");

                serieCorrelativo.Correlativo++;

                cabecera.Correlativo = serieCorrelativo.Correlativo;

                await _context.ComprobanteCabecera.AddAsync(cabecera);

                await _context.SaveChangesAsync();


                serieColletativoCreado = $"{cabecera.Serie}-{cabecera.Correlativo.ToString().PadLeft(7, '0')}";

                var detalle = _mapper.Map<List<ComprobanteDetalle>>(payload.DetalleComprobante);


                foreach (var item in detalle)
                {
                    item.ComprobanteCabeceraId = cabecera.Id;

                    item.ValorUnitarioTotal = item.Cantidad * item.ValorUnitario;

                    item.ValorIgv = (item.Cantidad * item.ValorUnitario) - ((item.Cantidad * item.ValorUnitario) / factor);

                    var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                    if (producto == null)
                        return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                    if ((producto.Stock ?? 0) < item.Cantidad)
                        return (ServiceStatus.FailedValidation, null, $"No hay stock disponible para el producto {producto.Nombre}");

                    var stockAnterior = producto.Stock ?? 0;
                    producto.Stock = stockAnterior - item.Cantidad;

                    _context.InventoryMovement.Add(new InventoryMovement
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = (int)TipoMovimientoInventario.Venta,
                        Cantidad = item.Cantidad,
                        StockAnterior = stockAnterior,
                        StockPosterior = producto.Stock.Value,
                        ReferenciaTipo = "Venta",
                        ReferenciaId = cabecera.Id
                    });
                }

                await _context.ComprobanteDetalle.AddRangeAsync(detalle);

                await _context.SaveChangesAsync();

                var pagos = _mapper.Map<List<Pago>>(payload.DetallePago);

                pagos.ForEach(x => x.ComprobanteCabeceraId = cabecera.Id);

                await _context.Pago.AddRangeAsync(pagos);

                await _context.SaveChangesAsync();


                await _context.Database.CommitTransactionAsync();
            }
            catch (Exception e)
            {
                await _context.Database.RollbackTransactionAsync();

                return (ServiceStatus.FailedValidation, null, $"Error al Crear <> {e.InnerException?.Message ?? e.Message}");
            }

            return (ServiceStatus.Ok, new
            {
                serieCorrelativo = serieColletativoCreado
            }, "Comprobante creado correctamente");
        }

        public async Task<(ServiceStatus, object, string)> ListarComprobantes(ComprobanteQueryParams queryparam)
        {

            DateTime start = new DateTime();
            DateTime end = new DateTime();

            var isValidStartDate = DateTime.TryParse(queryparam.StartDate, out start);
            var isValidEndDate = DateTime.TryParse(queryparam.EndDate, out end);

            if (!string.IsNullOrEmpty(queryparam.StartDate) && !isValidStartDate)

                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {queryparam.StartDate}");

            if (!string.IsNullOrEmpty(queryparam.EndDate) && !isValidEndDate)

                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {queryparam.StartDate}");


            DataCollection<ComprobanteCabeceraDTO> lista = null;


            lista = await _context.ComprobanteCabecera.AsNoTracking()
                                                      .WhereIf(string.IsNullOrEmpty(queryparam.StartDate) && string.IsNullOrEmpty(queryparam.EndDate), s => s.FechaCreacion.Date >= DateTime.UtcNow.AddHours(-5).AddDays(-7).Date)
                                                      .WhereIf(isValidStartDate && isValidEndDate, p => p.FechaCreacion.Date >= start.Date && p.FechaCreacion.Date <= end.Date)
                                                      .OrderByDescending(x => x.FechaCreacion)
                                                      .ProjectTo<ComprobanteCabeceraDTO>(_mapper.ConfigurationProvider)
                                                      .GetPagedAsync(queryparam.Page, queryparam.Amount);


            if (!lista.HasItems) return (ServiceStatus.NotFound, null, "No hay registros para mostrar");


            foreach (var (item, index) in lista.Items.WithCustomIndex())
            {
                item.Index = (queryparam.Page * queryparam.Amount) - queryparam.Amount + index;
            }

            return (ServiceStatus.Ok, lista, "Comprobantes listados correctamente");
        }

        //JOB
        public async Task<(ServiceStatus, List<ComprobanteCabecera>?)> ListarComprobantesAnulados(string tenant)
        {


            try
            {
                var lista = await _context.ComprobanteCabecera.Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                              .AsNoTracking()
                                                              .IgnoreQueryFilters()
                                                              .Where(x => x.TenantId == tenant &&
                                                                          x.EstadoComprobante == EstatusComprobante.Anulado &&
                                                                          x.EnviadoSunat == EstatusEnvioSunat.Enviado &&
                                                                          x.EnvioAnulacionSunat == false)
                                                              .OrderByDescending(x => x.FechaCreacion)
                                                              .ToListAsync();


                if (lista == null || lista.Count == 0) return (ServiceStatus.NotFound, null);

                return (ServiceStatus.Ok, lista);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null);
            }
        }
        //JOB
        public async Task<(ServiceStatus, List<ComprobanteCabecera>?)> ListarComprobantesPendientesEnviarSunat(string tenant)
        {


            try
            {
                var lista = await _context.ComprobanteCabecera.Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                              .AsNoTracking()
                                                              .IgnoreQueryFilters()
                                                              .Where(x => x.TenantId == tenant &&
                                                                          x.EstadoComprobante == EstatusComprobante.Creado &&
                                                                          x.EnviadoSunat == EstatusEnvioSunat.Pendiente)
                                                              .OrderByDescending(x => x.FechaCreacion)
                                                              .ToListAsync();


                if (lista == null || lista.Count == 0) return (ServiceStatus.NotFound, null);

                return (ServiceStatus.Ok, lista);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null);
            }
        }

        //JOB
        public async Task<(ServiceStatus, bool)> ActualizarComprobanteAEnviado(int idComprobante, string message)
        {
            try
            {
                var entity = await _context.ComprobanteCabecera.AsTracking()
                                                              .IgnoreQueryFilters()
                                                                .FirstOrDefaultAsync(x => x.Id == idComprobante);

                if (entity == null) return (ServiceStatus.NotFound, false);

                entity.EstadoComprobante = EstatusComprobante.Facturado;
                entity.EnviadoSunat = EstatusEnvioSunat.Enviado;
                entity.MensajeSunat = message;

                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, true);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, false);
            }
        }

        //JOB
        public async Task<(ServiceStatus, bool)> ActualizarComprobanteAAnuladoDesdeSunat(int idComprobante, string ticket)
        {
            try
            {
                var entity = await _context.ComprobanteCabecera.AsTracking()
                                                              .IgnoreQueryFilters()
                                                                .FirstOrDefaultAsync(x => x.Id == idComprobante);

                if (entity == null) return (ServiceStatus.NotFound, false);

                entity.EnvioAnulacionSunat = true;
                entity.TicketSunat = ticket;

                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, true);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, false);
            }
        }

        //JOB
        public async Task<(ServiceStatus, CorrelativoAnulacion)> ObtenerCorrelativoAnulacion(string tenant)
        {
            try
            {
                var datenow = DateTime.UtcNow.AddHours(-5);

                var entity = await _context.CorrelativoAnulacion.AsNoTracking()
                                                                .IgnoreQueryFilters()
                                                                .Where(x => x.FechaCreacion.Date == datenow.Date && x.TenantId == tenant)
                                                                .FirstOrDefaultAsync();

                if (entity == null)
                {
                    var correlativo = new CorrelativoAnulacion
                    {
                        Correlativo = 1,
                        FechaCreacion = datenow
                    };

                    await _context.CorrelativoAnulacion.AddAsync(correlativo);

                    await _context.SaveChangesAsync();

                    return (ServiceStatus.Ok, correlativo);
                }
                else
                {
                    return (ServiceStatus.Ok, entity);
                }
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null);
            }
        }

        //JOB
        public async Task<(ServiceStatus, bool)> ActualizarCorrelativoAnulacion(int id)
        {
            try
            {

                var entity = await _context.CorrelativoAnulacion.AsTracking()
                                                                .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) return (ServiceStatus.NotFound, false);

                entity.Correlativo++;

                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, true);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, false);
            }
        }

        //JOB
        public async Task<(ServiceStatus, bool)> ActualizarComprobanteAError(int idComprobante, string errorMessage)
        {
            try
            {
                var entity = await _context.ComprobanteCabecera.AsTracking()
                                                               .IgnoreQueryFilters()
                                                               .FirstOrDefaultAsync(x => x.Id == idComprobante);

                if (entity == null) return (ServiceStatus.NotFound, false);

                entity.EnviadoSunat = EstatusEnvioSunat.Error;
                entity.MensajeSunat = errorMessage;

                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, true);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, false);
            }
        }

        //JOB
        public async Task<(ServiceStatus, List<ConfiguracionFiscal>?)> ObtenerConfiguracionesFiscalesActivas()
        {
            try
            {
                var lista = await _context.ConfiguracionFiscal
                                          .Include(x => x.Empresa)
                                          .IgnoreQueryFilters()
                                          .Where(x => x.Activo && x.Estado)
                                          .ToListAsync();

                return (ServiceStatus.Ok, lista);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null);
            }
        }

        //JOB
        public async Task<(ServiceStatus, ConfiguracionFiscal?)> ObtenerConfiguracionFiscalPorTenant(string tenant)
        {
            try
            {
                var config = await _context.ConfiguracionFiscal
                                           .Include(x => x.Empresa)
                                           .IgnoreQueryFilters()
                                           .AsNoTracking()
                                           .Where(x => x.TenantId == tenant && x.Activo && x.Estado)
                                           .FirstOrDefaultAsync();

                return (ServiceStatus.Ok, config);
            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null);
            }
        }



        public async Task<(ServiceStatus, InvoiceRequest?, string)> GeneratePdfRequest(int idComprobante)
        {

            try
            {

                var entity = await _context.ComprobanteCabecera.Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                               .AsNoTracking()
                                                               .IgnoreQueryFilters()
                                                               .FirstOrDefaultAsync(x => x.Id == idComprobante);

                if (entity == null)

                    return (ServiceStatus.FailedValidation, null, "no se encontró comprobante");

                var (_, config) = await ObtenerConfiguracionFiscalPorTenant(entity.TenantId);

                var request = ArmarInvoice(entity, config);

                return (ServiceStatus.Ok, request, "succeeded");

            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<(ServiceStatus, object?, string)> VentasRealizadas(string fecha)
        {

            var user = _httpContextAccessor!.HttpContext!.User.FindFirstValue("username").ToUpper();

            DateTime start = new DateTime();

            if (!string.IsNullOrEmpty(fecha) && !DateTime.TryParse(fecha, out start))

                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {fecha}");


            try
            {

                var rentas = await _context.ComprobanteCabecera.AsNoTracking()
                                                           .WhereIf(user != null, rc => rc.FechaCreacion.Date == start.Date && rc.UsuarioCreacion == user)
                                                           .Select(q => new
                                                           {
                                                               monto = q.ValorTotal,
                                                               fecha = q.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss"),
                                                               totalMasCuarto = string.Empty,
                                                           })
                                                         .ToListAsync();

                if (rentas.Count() == 0)
                    return (ServiceStatus.FailedValidation, null, "No se encontraron reporte rentas");

                return (ServiceStatus.Ok, rentas, "Success");

            }
            catch (Exception ex)
            {
                return (ServiceStatus.FailedValidation, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        public async Task<(ServiceStatus, object?, string)> AnularVenta(int IdComprobante, string motivo)
        {
            try
            {
                var entity = await _context.ComprobanteCabecera.AsNoTracking().FirstAsync(p => p.Id == IdComprobante);

                entity.EstadoComprobante = EstatusComprobante.Anulado;

                entity.MotivoAnulacion = motivo;

                _context.Entry(entity).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return (ServiceStatus.Ok, null, "Success");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.FailedValidation, null, $"Error en Anular Venta -> {ex.InnerException?.Message ?? ex.Message}");
            }

        }

        private InvoiceRequest ArmarInvoice(ComprobanteCabecera comprobanteCabecera, ConfiguracionFiscal? config)
        {

            var cliente = new Client
            {
                tipoDoc = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "0" : comprobanteCabecera.NumeroDocumento.Length == 11 ? "6" : "1",
                numDoc = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "00000000" : comprobanteCabecera.NumeroDocumento,
                rznSocial = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "SIN NOMBRE" : comprobanteCabecera.RazonSocial,
                //address = null
            };

            var moneda = config?.Moneda ?? "PEN";

            var paisId = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } paisClaim
                            && int.TryParse(paisClaim, out var parsedPais) ? parsedPais : (int?)null;

            var impuesto = _taxCalculatorFactory.GetCalculator(paisId)
                                                .GetImpuestoRate(paisId, config?.PorcentajeImpuesto);
            var factor = 1m + (impuesto / 100m);

            var company = new Company
            {
                ruc = long.Parse(config?.Ruc ?? "0"),
                razonSocial = config?.RazonSocial ?? "EMPRESA NO CONFIGURADA",
                nombreComercial = config?.NombreComercial ?? config?.RazonSocial,
                address = new Address
                {
                    ubigueo = config?.UbigeoId ?? "000000",
                    departamento = config?.Departamento ?? "",
                    provincia = config?.Provincia ?? "",
                    distrito = config?.Distrito ?? "",
                    direccion = config?.Direccion ?? ""
                }
            };

            var serie = comprobanteCabecera.TipoDocumentoVentaId == (int)TipoComprobante.Factura
                ? config?.SerieFactura ?? "F001"
                : config?.SerieBoleta ?? "B001";

            string fechaHoraFormateada = comprobanteCabecera.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:sszzz");

            var invoice = new InvoiceRequest
            {
                ublVersion = "2.1",
                tipoOperacion = "0101",
                tipoDoc = comprobanteCabecera.TipoDocumentoVentaId == (int)TipoComprobante.Factura ? "01" : "03",
                serie = serie,
                correlativo = comprobanteCabecera.Correlativo.ToString().PadLeft(7, '0'),
                fechaEmision = fechaHoraFormateada,
                formaPago = new FormaPago { tipo = "Contado", moneda = moneda },
                tipoMoneda = moneda,
                client = cliente,
                company = company,
                subTotal = comprobanteCabecera.ValorTotal,
                mtoImpVenta = comprobanteCabecera.ValorTotal,
                mtoOperGravadas = comprobanteCabecera.ValorSubtotal,
                valorVenta = comprobanteCabecera.ValorSubtotal,
                mtoIGV = comprobanteCabecera.ValorIgv,
                totalImpuestos = comprobanteCabecera.ValorIgv,
                details = comprobanteCabecera.ComprobanteDetalles.Select(x => new Detail
                {
                    unidad = "NIU",
                    codProducto = "P001",
                    cantidad = x.Cantidad,
                    descripcion = x.Producto.Nombre,
                    mtoValorUnitario = Math.Round(x.ValorUnitario / factor, 2),
                    mtoValorVenta = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                    mtoBaseIgv = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                    porcentajeIgv = impuesto,
                    igv = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                    tipAfeIgv = "10",
                    totalImpuestos = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                    mtoPrecioUnitario = x.ValorUnitario,
                }).ToList(),
                legends = new List<Legend>
                {
                    new Legend
                    {
                        code = "1000",
                        value = comprobanteCabecera.TotalLetras
                    }
                }
            };

            return invoice;

        }

    }
}
