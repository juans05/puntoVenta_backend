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

                // El total llega desde JS (suma en punto flotante, ej. 121.80000000000001) --
                // se redondea a centimos antes de comparar contra la suma exacta en decimal.
                if (Math.Round(payload.Total, 2) != Math.Round(sumatoria, 2))
                    return (ServiceStatus.FailedValidation, null, "El total no coincide con la suma de los detalles");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Factura && string.IsNullOrEmpty(payload.NumeroDocumento))
                    return (ServiceStatus.FailedValidation, null, "Por favor, ingrese el numero de ruc");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Factura && string.IsNullOrEmpty(payload.RazonSocial))
                    return (ServiceStatus.FailedValidation, null, "Por favor, envie el nombre de la Razon Social");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Factura && payload.NumeroDocumento.Length != 11)
                    return (ServiceStatus.FailedValidation, null, "Por favor, indique un numero de ruc valido");


                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Boleta && !string.IsNullOrEmpty(payload.NumeroDocumento) && string.IsNullOrEmpty(payload.RazonSocial))
                    return (ServiceStatus.FailedValidation, null, "Por favor, ingrese el nombre");


                // Normaliza a centimos una sola vez: payload.Total llega desde JS (puede traer
                // ruido de punto flotante) y todo lo que sigue se calcula a partir de este valor.
                payload.Total = Math.Round(payload.Total, 2);

                var cabecera = _mapper.Map<ComprobanteCabecera>(payload);

                cabecera.FechaVenta = payload.FechaVenta ?? DateTime.UtcNow.AddHours(-5);

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
                    cabecera.Serie = config?.SerieFactura ?? "F001";
                else if (payload.TipoDocumentoVentaId == 2)
                    cabecera.Serie = config?.SerieBoleta ?? "B001";
                else
                    cabecera.Serie = config?.SerieNota ?? "RC01";


                var serieCorrelativo = await _context.Seriecorrelativo.Where(x => x.Serie == cabecera.Serie
                                                                              && x.TipoDocumentoVentaId == payload.TipoDocumentoVentaId)
                                                                      .AsTracking()
                                                                      .FirstOrDefaultAsync();

                if (serieCorrelativo == null)
                {
                    // Falta si el tenant/sucursal no llegó a sembrarse (tenant nuevo, sucursal
                    // creada luego del seed inicial): se crea aquí en vez de bloquear la venta.
                    serieCorrelativo = new Seriecorrelativo
                    {
                        Serie = cabecera.Serie,
                        TipoDocumentoVentaId = payload.TipoDocumentoVentaId,
                        Correlativo = 0
                    };
                    await _context.Seriecorrelativo.AddAsync(serieCorrelativo);
                }

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
                                                      .Where(x => x.EstadoComprobante != EstatusComprobante.Anulado)
                                                      .WhereIf(string.IsNullOrEmpty(queryparam.StartDate) && string.IsNullOrEmpty(queryparam.EndDate), s => (s.FechaVenta ?? s.FechaCreacion).Date >= DateTime.UtcNow.AddHours(-5).AddDays(-7).Date)
                                                      .WhereIf(isValidStartDate && isValidEndDate, p => (p.FechaVenta ?? p.FechaCreacion).Date >= start.Date && (p.FechaVenta ?? p.FechaCreacion).Date <= end.Date)
                                                      .OrderByDescending(x => x.FechaVenta ?? x.FechaCreacion)
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
                // La Nota de venta no es un comprobante fiscal (no tiene tipoDoc SUNAT propio) y
                // nunca debe enviarse a SUNAT -- solo Boleta/Factura pasan por este job.
                var lista = await _context.ComprobanteCabecera.Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                              .AsNoTracking()
                                                              .IgnoreQueryFilters()
                                                              .Where(x => x.TenantId == tenant &&
                                                                          x.EstadoComprobante == EstatusComprobante.Creado &&
                                                                          x.EnviadoSunat == EstatusEnvioSunat.Pendiente &&
                                                                          (x.TipoDocumentoVentaId == (int)TipoComprobante.Factura ||
                                                                           x.TipoDocumentoVentaId == (int)TipoComprobante.Boleta))
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
                                                           .WhereIf(user != null, rc => (rc.FechaVenta ?? rc.FechaCreacion).Date == start.Date && rc.UsuarioCreacion == user)
                                                           .Select(q => new
                                                           {
                                                               monto = q.ValorTotal,
                                                               fecha = (q.FechaVenta ?? q.FechaCreacion).ToString("dd/MM/yyyy HH:mm:ss"),
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
            var entity = await _context.ComprobanteCabecera.AsTracking()
                                                            .Include(c => c.ComprobanteDetalles)
                                                            .FirstOrDefaultAsync(p => p.Id == IdComprobante);

            if (entity == null)
                return (ServiceStatus.FailedValidation, null, $"No se encontro el comprobante {IdComprobante}");

            if (entity.EstadoComprobante == EstatusComprobante.Anulado)
                return (ServiceStatus.FailedValidation, null, "La venta ya se encuentra anulada");

            await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in entity.ComprobanteDetalles)
                {
                    var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                    if (producto == null) continue;

                    var stockAnterior = producto.Stock ?? 0;
                    var stockNuevo = stockAnterior + item.Cantidad;

                    producto.Stock = stockNuevo;

                    _context.InventoryMovement.Add(new InventoryMovement
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = (int)TipoMovimientoInventario.DevolucionVenta,
                        Cantidad = item.Cantidad,
                        StockAnterior = stockAnterior,
                        StockPosterior = stockNuevo,
                        ReferenciaTipo = "VentaAnulada",
                        ReferenciaId = entity.Id
                    });
                }

                entity.EstadoComprobante = EstatusComprobante.Anulado;

                entity.MotivoAnulacion = motivo;

                await _context.SaveChangesAsync();

                await _context.Database.CommitTransactionAsync();

                return (ServiceStatus.Ok, null, "Success");
            }
            catch (Exception ex)
            {
                await _context.Database.RollbackTransactionAsync();

                return (ServiceStatus.FailedValidation, null, $"Error en Anular Venta -> {ex.InnerException?.Message ?? ex.Message}");
            }

        }

        public async Task<(ServiceStatus, string)> ActualizarFechaVenta(int id, DateTime fecha)
        {
            var comprobante = await _context.ComprobanteCabecera.AsTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (comprobante == null)
                return (ServiceStatus.NotFound, $"No se encontro el comprobante {id}");

            if (comprobante.EstadoComprobante == EstatusComprobante.Anulado)
                return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de un comprobante anulado");

            comprobante.FechaVenta = fecha;

            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Fecha de venta actualizada correctamente");
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
