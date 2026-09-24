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
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
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


                // Factura de un pedido de venta (flujo completo): solo lo entregado y sin facturar, y el
                // stock ya bajo en la entrega. El PedidoVentaId del cliente solo cuenta si pasa la validacion.
                if (payload.PedidoVentaId.HasValue
                    && await PedidoVentaFacturacion.Validar(_context, payload) is { } rechazoPedido)
                    return (ServiceStatus.FailedValidation, null, rechazoPedido);

                // Normaliza a centimos una sola vez: payload.Total llega desde JS (puede traer
                // ruido de punto flotante) y todo lo que sigue se calcula a partir de este valor.
                payload.Total = Math.Round(payload.Total, 2);

                // El frontend a veces solo envia NumeroDocumento/RazonSocial como texto libre
                // (sin buscar ni seleccionar un Cliente existente), asi que el ComprobanteCabecera
                // quedaba sin ClienteId aunque el nombre/DNI si se hubiera guardado -- por eso se
                // veia "sin especificar" en pantallas que dependen del Cliente vinculado. Se
                // resuelve/crea el Cliente aqui, una sola vez, para que cualquier flujo de venta
                // (Facturacion, Venta rapida, Nueva Factura) quede siempre vinculado.
                if (payload.ClienteId == null && !string.IsNullOrWhiteSpace(payload.NumeroDocumento))
                {
                    var clienteExistente = await _context.Cliente.AsTracking()
                        .FirstOrDefaultAsync(c => c.NumeroDocumento == payload.NumeroDocumento);

                    if (clienteExistente != null)
                    {
                        payload.ClienteId = clienteExistente.Id;
                        if (string.IsNullOrWhiteSpace(payload.RazonSocial))
                            payload.RazonSocial = clienteExistente.Nombre;

                        // Datos capturados en esta venta completan/actualizan al Cliente ya
                        // existente (ej. agrego direccion/email que antes no tenia).
                        if (!string.IsNullOrWhiteSpace(payload.RazonSocial)) clienteExistente.Nombre = payload.RazonSocial;
                        if (payload.TipoDocumentoId.HasValue) clienteExistente.TipoDocumentoId = payload.TipoDocumentoId;
                        if (!string.IsNullOrWhiteSpace(payload.DireccionCliente)) clienteExistente.Direccion = payload.DireccionCliente;
                        if (!string.IsNullOrWhiteSpace(payload.UbigeoId)) clienteExistente.UbigeoId = payload.UbigeoId;
                        if (!string.IsNullOrWhiteSpace(payload.Celular)) clienteExistente.Telefono = payload.Celular;
                        if (!string.IsNullOrWhiteSpace(payload.Email)) clienteExistente.Email = payload.Email;
                    }
                    else if (!string.IsNullOrWhiteSpace(payload.RazonSocial))
                    {
                        var clienteNuevo = new Cliente
                        {
                            NumeroDocumento = payload.NumeroDocumento,
                            Nombre = payload.RazonSocial,
                            TipoDocumentoId = payload.TipoDocumentoId ?? (payload.NumeroDocumento.Length == 11 ? 5 : 1), // RUC : DNI (ver tipodocumento.json)
                            Direccion = payload.DireccionCliente,
                            UbigeoId = payload.UbigeoId,
                            Telefono = payload.Celular,
                            Email = payload.Email
                        };

                        await _context.Cliente.AddAsync(clienteNuevo);
                        await _context.SaveChangesAsync();

                        payload.ClienteId = clienteNuevo.Id;
                    }
                }

                var cabecera = _mapper.Map<ComprobanteCabecera>(payload);

                cabecera.StockYaDescontado = payload.PedidoVentaId.HasValue;

                cabecera.FechaVenta = payload.FechaVenta ?? DateTime.UtcNow.AddHours(-5);

                var (_, config) = await ObtenerConfiguracionFiscalPorTenant(_context.CurrentTenantName);

                var paisId = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } paisClaim
                            && int.TryParse(paisClaim, out var parsedPais) ? parsedPais : (int?)null;

                var impuesto = _taxCalculatorFactory.GetCalculator(paisId)
                                                    .GetImpuestoRate(paisId, config?.PorcentajeImpuesto);
                var factor = 1m + (impuesto / 100m);

                var detalle = _mapper.Map<List<ComprobanteDetalle>>(payload.DetalleComprobante);

                // El IGV se calcula por linea (cada una puede ser Gravada, Exonerada o Inafecta
                // segun su TipoIgv) y la cabecera resulta de sumar las lineas -- si ninguna linea
                // trae TipoIgvId (flujos viejos como Facturacion/ModalPay) todas caen en "Gravado",
                // dando el mismo resultado que el calculo global de antes.
                var tipoIgvIds = detalle.Where(d => d.TipoIgvId.HasValue).Select(d => d.TipoIgvId!.Value).Distinct().ToList();
                var tiposIgv = tipoIgvIds.Count > 0
                    ? await _context.TipoIgv.AsNoTracking().Where(t => tipoIgvIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id)
                    : new Dictionary<int, TipoIgv>();

                var subtotalCalculado = 0m;

                foreach (var item in detalle)
                {
                    var aplicaImpuesto = !item.TipoIgvId.HasValue
                        || !tiposIgv.TryGetValue(item.TipoIgvId.Value, out var tipoIgv)
                        || tipoIgv.AplicaPorcentajeImpuesto;

                    var importeLinea = item.Cantidad * item.ValorUnitario;

                    item.ValorUnitarioTotal = importeLinea;
                    item.ValorIgv = aplicaImpuesto ? importeLinea - (importeLinea / factor) : 0m;

                    subtotalCalculado += importeLinea - item.ValorIgv;
                }

                cabecera.ValorTotal = payload.Total;

                cabecera.ValorSubtotal = Math.Round(subtotalCalculado, 2);

                cabecera.ValorIgv = Math.Round(payload.Total - subtotalCalculado, 2);

                cabecera.PorcentajeImpuesto = impuesto;

                cabecera.TotalLetras = DecimalExtensions.ConvertirNumeroALetras(payload.Total);

                cabecera.ModoEnvio = payload.ModoEnvio;

                // "Solo Firmar e Imprimir" y "Solo Guardar la Venta" no deben pasar por el job
                // que envia a SUNAT -- solo "Enviar a SUNAT ahora" (o no elegir modo, default legacy)
                // deja el comprobante en Pendiente para que el job lo recoja.
                if (payload.ModoEnvio == EstatusModoEnvio.SoloFirmarImprimir || payload.ModoEnvio == EstatusModoEnvio.SoloGuardar)
                    cabecera.EnviadoSunat = EstatusEnvioSunat.NoEnviar;

                // Si la venta trae sucursal y esa sucursal tiene su propia serie configurada, se usa esa
                // -- si no, cae al comportamiento de siempre (serie unica del tenant). El contador de
                // correlativo ya se busca por el texto de la Serie (mas abajo), asi que dos sucursales
                // con series distintas quedan numeradas por separado sin tocar esa logica.
                var sucursalVenta = payload.SucursalId.HasValue
                    ? await _context.Sucursal.AsNoTracking().FirstOrDefaultAsync(s => s.Id == payload.SucursalId)
                    : null;

                if (payload.TipoDocumentoVentaId == 1)
                    cabecera.Serie = sucursalVenta?.SerieFactura ?? config?.SerieFactura ?? "F001";
                else if (payload.TipoDocumentoVentaId == 2)
                    cabecera.Serie = sucursalVenta?.SerieBoleta ?? config?.SerieBoleta ?? "B001";
                else if (payload.TipoDocumentoVentaId == (int)TipoComprobante.Cotizacion)
                    cabecera.Serie = config?.SerieCotizacion ?? "COT01";
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

                // Cotizacion es un precio tentativo, no una venta -- no descuenta stock ni exige
                // disponibilidad (el cliente todavia no confirmo nada).
                var esCotizacion = payload.TipoDocumentoVentaId == (int)TipoComprobante.Cotizacion;

                foreach (var item in detalle)
                {
                    item.ComprobanteCabeceraId = cabecera.Id;

                    var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                    if (producto == null)
                        return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                    if (esCotizacion || cabecera.StockYaDescontado) continue;

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

                if (cabecera.StockYaDescontado)
                    await PedidoVentaFacturacion.Aplicar(_context, payload.PedidoVentaId!.Value,
                        detalle.Select(d => (d.ProductoId, d.Cantidad)), +1);

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

            var isValidRegistroInicio = DateTime.TryParse(queryparam.FechaRegistroInicio, out var registroInicio);
            var isValidRegistroFin = DateTime.TryParse(queryparam.FechaRegistroFin, out var registroFin);

            if (!string.IsNullOrEmpty(queryparam.FechaRegistroInicio) && !isValidRegistroInicio)
                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {queryparam.FechaRegistroInicio}");

            if (!string.IsNullOrEmpty(queryparam.FechaRegistroFin) && !isValidRegistroFin)
                return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {queryparam.FechaRegistroFin}");


            DataCollection<ComprobanteCabeceraDTO> lista = null;


            lista = await _context.ComprobanteCabecera.AsNoTracking()
                                                      .Where(x => x.EstadoComprobante != EstatusComprobante.Anulado)
                                                      .WhereIf(string.IsNullOrEmpty(queryparam.StartDate) && string.IsNullOrEmpty(queryparam.EndDate), s => (s.FechaVenta ?? s.FechaCreacion).Date >= DateTime.UtcNow.AddHours(-5).AddDays(-7).Date)
                                                      .WhereIf(isValidStartDate && isValidEndDate, p => (p.FechaVenta ?? p.FechaCreacion).Date >= start.Date && (p.FechaVenta ?? p.FechaCreacion).Date <= end.Date)
                                                      .WhereIf(isValidRegistroInicio && isValidRegistroFin, p => p.FechaCreacion.Date >= registroInicio.Date && p.FechaCreacion.Date <= registroFin.Date)
                                                      .WhereIf(!string.IsNullOrWhiteSpace(queryparam.NumeroDocumento), p => p.NumeroDocumento != null && p.NumeroDocumento.Contains(queryparam.NumeroDocumento))
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
                // nunca debe enviarse a SUNAT -- solo Boleta/Factura/NotaCredito/NotaDebito pasan por este job.
                var lista = await _context.ComprobanteCabecera.Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                              .Include(x => x.ComprobanteDetalles).ThenInclude(x => x.TipoIgv)
                                                              .Include(x => x.ComprobanteDetalles).ThenInclude(x => x.UnidadMedida)
                                                              .Include(x => x.ComprobanteAfectado)
                                                              .Include(x => x.MotivoNota)
                                                              .AsNoTracking()
                                                              .IgnoreQueryFilters()
                                                              .Where(x => x.TenantId == tenant &&
                                                                          x.EstadoComprobante == EstatusComprobante.Creado &&
                                                                          x.EnviadoSunat == EstatusEnvioSunat.Pendiente &&
                                                                          (x.TipoDocumentoVentaId == (int)TipoComprobante.Factura ||
                                                                           x.TipoDocumentoVentaId == (int)TipoComprobante.Boleta ||
                                                                           x.TipoDocumentoVentaId == (int)TipoComprobante.NotaCredito ||
                                                                           x.TipoDocumentoVentaId == (int)TipoComprobante.NotaDebito))
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
        // Series configuradas para el tenant actual, con el mismo fallback que usa CrearComprobante --
        // permite mostrar en pantalla que serie se va a usar antes de emitir el documento.
        public async Task<(ServiceStatus, object?, string)> ObtenerSeriesDocumento()
        {
            var (_, config) = await ObtenerConfiguracionFiscalPorTenant(_context.CurrentTenantName);

            var series = new
            {
                factura = config?.SerieFactura ?? "F001",
                boleta = config?.SerieBoleta ?? "B001",
                notaVenta = config?.SerieNota ?? "RC01",
                cotizacion = config?.SerieCotizacion ?? "COT01"
            };

            return (ServiceStatus.Ok, series, "Success");
        }

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
                                                               .Include(x => x.ComprobanteDetalles).ThenInclude(x => x.TipoIgv)
                                                               .Include(x => x.ComprobanteDetalles).ThenInclude(x => x.UnidadMedida)
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
                // Si el stock bajo en una entrega, anular la factura no lo devuelve: solo reabre lo facturado.
                if (entity.StockYaDescontado && entity.PedidoVentaId.HasValue)
                    await PedidoVentaFacturacion.Aplicar(_context, entity.PedidoVentaId.Value,
                        entity.ComprobanteDetalles.Select(d => (d.ProductoId, d.Cantidad)), -1);
                else
                    await RestaurarStock(entity.ComprobanteDetalles, entity.Id, "VentaAnulada");

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

        private async Task RestaurarStock(IEnumerable<ComprobanteDetalle> detalles, int comprobanteId, string referenciaTipo)
        {
            foreach (var item in detalles)
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
                    ReferenciaTipo = referenciaTipo,
                    ReferenciaId = comprobanteId
                });
            }
        }

        public async Task<(ServiceStatus, object?, string)> CrearNotaCreditoDebito(NotaPayload payload)
        {
            if (payload.TipoDocumentoVentaId != (int)TipoComprobante.NotaCredito && payload.TipoDocumentoVentaId != (int)TipoComprobante.NotaDebito)
                return (ServiceStatus.FailedValidation, null, "Tipo de nota invalido");

            var afectado = await _context.ComprobanteCabecera.AsTracking()
                                                              .Include(c => c.ComprobanteDetalles)
                                                              .FirstOrDefaultAsync(p => p.Id == payload.ComprobanteAfectadoId);

            if (afectado == null)
                return (ServiceStatus.FailedValidation, null, $"No se encontro el comprobante {payload.ComprobanteAfectadoId}");

            if (afectado.EstadoComprobante == EstatusComprobante.Anulado)
                return (ServiceStatus.FailedValidation, null, "El comprobante afectado se encuentra anulado");

            if (afectado.TipoDocumentoVentaId != (int)TipoComprobante.Factura && afectado.TipoDocumentoVentaId != (int)TipoComprobante.Boleta)
                return (ServiceStatus.FailedValidation, null, "Solo se puede emitir una nota contra una Factura o Boleta");

            var motivo = await _context.MotivoNota.AsNoTracking().FirstOrDefaultAsync(m => m.Id == payload.MotivoNotaId);

            if (motivo == null)
                return (ServiceStatus.FailedValidation, null, $"No se encontro el motivo {payload.MotivoNotaId}");

            if (motivo.TipoDocumentoVentaId != payload.TipoDocumentoVentaId)
                return (ServiceStatus.FailedValidation, null, "El motivo no corresponde al tipo de nota seleccionado");

            await _context.Database.BeginTransactionAsync();

            var serieColletativoCreado = string.Empty;

            try
            {
                var (_, config) = await ObtenerConfiguracionFiscalPorTenant(_context.CurrentTenantName);

                var cabecera = new ComprobanteCabecera
                {
                    TipoDocumentoVentaId = payload.TipoDocumentoVentaId,
                    ClienteId = afectado.ClienteId,
                    NumeroDocumento = afectado.NumeroDocumento,
                    RazonSocial = afectado.RazonSocial,
                    ValorTotal = afectado.ValorTotal,
                    ValorSubtotal = afectado.ValorSubtotal,
                    ValorIgv = afectado.ValorIgv,
                    PorcentajeImpuesto = afectado.PorcentajeImpuesto,
                    TotalLetras = afectado.TotalLetras,
                    FechaVenta = DateTime.UtcNow.AddHours(-5),
                    ComprobanteAfectadoId = afectado.Id,
                    MotivoNotaId = motivo.Id
                };

                cabecera.Serie = payload.TipoDocumentoVentaId == (int)TipoComprobante.NotaCredito
                    ? config?.SerieNotaCredito ?? "FC01"
                    : config?.SerieNotaDebito ?? "FD01";

                var serieCorrelativo = await _context.Seriecorrelativo.Where(x => x.Serie == cabecera.Serie
                                                                              && x.TipoDocumentoVentaId == payload.TipoDocumentoVentaId)
                                                                      .AsTracking()
                                                                      .FirstOrDefaultAsync();

                if (serieCorrelativo == null)
                {
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

                var detalleNuevo = afectado.ComprobanteDetalles.Select(d => new ComprobanteDetalle
                {
                    ComprobanteCabeceraId = cabecera.Id,
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    ValorUnitario = d.ValorUnitario,
                    ValorUnitarioTotal = d.ValorUnitarioTotal,
                    ValorIgv = d.ValorIgv,
                    CostoReal = d.CostoReal
                }).ToList();

                await _context.ComprobanteDetalle.AddRangeAsync(detalleNuevo);

                await _context.SaveChangesAsync();

                if (payload.TipoDocumentoVentaId == (int)TipoComprobante.NotaCredito && motivo.RevierteStock)
                    await RestaurarStock(detalleNuevo, cabecera.Id, "NotaCredito");

                await _context.SaveChangesAsync();

                await _context.Database.CommitTransactionAsync();
            }
            catch (Exception e)
            {
                await _context.Database.RollbackTransactionAsync();

                return (ServiceStatus.FailedValidation, null, $"Error al crear la nota <> {e.InnerException?.Message ?? e.Message}");
            }

            return (ServiceStatus.Ok, new
            {
                serieCorrelativo = serieColletativoCreado
            }, "Nota creada correctamente");
        }

        public async Task<(ServiceStatus, object?, string)> BuscarComprobantePorSerieCorrelativo(string serie, int correlativo)
        {
            var comprobante = await _context.ComprobanteCabecera.AsNoTracking()
                                                                 .Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                                 .Include(x => x.Cliente)
                                                                 .Include(x => x.TipoDocumentoVenta)
                                                                 .FirstOrDefaultAsync(x => x.Serie == serie && x.Correlativo == correlativo);

            if (comprobante == null)
                return (ServiceStatus.NotFound, null, "No se encontro el comprobante");

            if (comprobante.EstadoComprobante == EstatusComprobante.Anulado)
                return (ServiceStatus.FailedValidation, null, "El comprobante se encuentra anulado");

            return (ServiceStatus.Ok, comprobante, "Comprobante encontrado");
        }

        public async Task<(ServiceStatus, string?, string)> GenerarPdfCotizacion(int id)
        {
            var cotizacion = await _context.ComprobanteCabecera.AsNoTracking()
                                                                .Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                                .FirstOrDefaultAsync(x => x.Id == id && x.TipoDocumentoVentaId == (int)TipoComprobante.Cotizacion);

            if (cotizacion == null)
                return (ServiceStatus.NotFound, null, "No se encontro la cotizacion");

            var (_, config) = await ObtenerConfiguracionFiscalPorTenant(cotizacion.TenantId);

            var documento = new CotizacionPdfDocument(cotizacion, config);
            var bytes = documento.GeneratePdf();

            return (ServiceStatus.Ok, Convert.ToBase64String(bytes), "PDF generado correctamente");
        }

        public async Task<(ServiceStatus, object?, string)> ObtenerCotizacionParaConvertir(int id)
        {
            var cotizacion = await _context.ComprobanteCabecera.AsNoTracking()
                                                                .Include(x => x.ComprobanteDetalles).ThenInclude(x => x.Producto)
                                                                .Include(x => x.Cliente)
                                                                .FirstOrDefaultAsync(x => x.Id == id && x.TipoDocumentoVentaId == (int)TipoComprobante.Cotizacion);

            if (cotizacion == null)
                return (ServiceStatus.NotFound, null, "No se encontro la cotizacion");

            if (cotizacion.EstadoComprobante == EstatusComprobante.Anulado)
                return (ServiceStatus.FailedValidation, null, "La cotizacion se encuentra anulada");

            if (cotizacion.FechaVigencia.HasValue && cotizacion.FechaVigencia.Value.Date < DateTime.UtcNow.AddHours(-5).Date)
                return (ServiceStatus.FailedValidation, null, "La cotizacion esta vencida");

            var yaConvertida = await _context.ComprobanteCabecera.AsNoTracking()
                                                                  .AnyAsync(x => x.CotizacionOrigenId == id);

            if (yaConvertida)
                return (ServiceStatus.FailedValidation, null, "Esta cotizacion ya fue convertida");

            return (ServiceStatus.Ok, cotizacion, "Cotizacion lista para convertir");
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

        // Registro de Ventas e Ingresos (PLE 14.1) -- solo documentos fiscales reales (Factura,
        // Boleta, Nota de Credito, Nota de Debito). Nota de Venta y Cotizacion no son comprobantes
        // SUNAT y quedan fuera del libro.
        public async Task<(ServiceStatus, List<LibroVentaDto>?, string)> ObtenerLibroVentas(ContabilidadQueryParams payload)
        {
            try
            {
                var query = _context.ComprobanteCabecera.AsNoTracking()
                    .Include(c => c.Moneda)
                    .Include(c => c.ComprobanteAfectado)
                    .Include(c => c.ComprobanteDetalles).ThenInclude(d => d.TipoIgv)
                    .Where(c => new[] { 1, 2, 4, 5 }.Contains(c.TipoDocumentoVentaId));

                if (payload.SucursalId.HasValue)
                    query = query.Where(c => c.SucursalId == payload.SucursalId);

                if (DateTime.TryParse(payload.FechaInicio, out var inicio))
                    query = query.Where(c => c.FechaVenta >= inicio.Date);

                if (DateTime.TryParse(payload.FechaFin, out var fin))
                    query = query.Where(c => c.FechaVenta <= fin.Date.AddDays(1).AddTicks(-1));

                var cabeceras = await query.OrderBy(c => c.FechaVenta).ThenBy(c => c.Correlativo).ToListAsync();

                var libro = cabeceras.Select(c =>
                {
                    var gravada = c.ComprobanteDetalles.Where(d => d.TipoIgv == null || d.TipoIgv.AplicaPorcentajeImpuesto)
                                                        .Sum(d => d.ValorUnitarioTotal - d.ValorIgv);
                    var exonerada = c.ComprobanteDetalles.Where(d => d.TipoIgv != null && !d.TipoIgv.AplicaPorcentajeImpuesto && d.TipoIgv.Codigo == "20")
                                                          .Sum(d => d.ValorUnitarioTotal);
                    var inafecta = c.ComprobanteDetalles.Where(d => d.TipoIgv != null && !d.TipoIgv.AplicaPorcentajeImpuesto && d.TipoIgv.Codigo == "30")
                                                         .Sum(d => d.ValorUnitarioTotal);

                    return new LibroVentaDto
                    {
                        Periodo = c.FechaVenta?.ToString("yyyyMM") ?? "",
                        Cuo = c.Id.ToString().PadLeft(12, '0'),
                        FechaEmision = c.FechaVenta?.ToString("dd/MM/yyyy") ?? "",
                        FechaVencimiento = c.FechaVencimiento?.ToString("dd/MM/yyyy"),
                        TipoComprobante = SunatCodigos.TipoComprobante(c.TipoDocumentoVentaId),
                        Serie = c.Serie,
                        Numero = c.Correlativo.ToString().PadLeft(7, '0'),
                        TipoDocCliente = SunatCodigos.TipoDocumentoIdentidadPorNumero(c.NumeroDocumento),
                        NumeroDocCliente = c.NumeroDocumento,
                        RazonSocial = c.RazonSocial,
                        BaseImponibleGravada = Math.Round(gravada, 2),
                        DescuentoBaseImponible = c.MontoDescuento ?? 0,
                        OpExonerada = Math.Round(exonerada, 2),
                        OpInafecta = Math.Round(inafecta, 2),
                        Igv = c.ValorIgv,
                        ImporteTotal = c.ValorTotal,
                        Moneda = c.Moneda?.Codigo ?? "PEN",
                        TipoCambio = c.TipoCambio,
                        FechaDocModificado = c.ComprobanteAfectado?.FechaVenta?.ToString("dd/MM/yyyy"),
                        TipoDocModificado = c.ComprobanteAfectado != null ? SunatCodigos.TipoComprobante(c.ComprobanteAfectado.TipoDocumentoVentaId) : null,
                        SerieDocModificado = c.ComprobanteAfectado?.Serie,
                        NumeroDocModificado = c.ComprobanteAfectado?.Correlativo.ToString().PadLeft(7, '0'),
                        Estado = SunatCodigos.Estado(c.EstadoComprobante)
                    };
                }).ToList();

                return (ServiceStatus.Ok, libro, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
            }
        }

        // Una fila por producto vendido (no por documento) -- incluye Nota de Venta y Cotizacion,
        // a diferencia del libro electronico, porque aqui no se trata de cumplimiento SUNAT sino
        // de ver que se vendio.
        public async Task<(ServiceStatus, List<ReporteDetalladoVentaDto>?, string)> ObtenerReporteDetalladoVentas(ContabilidadQueryParams payload)
        {
            try
            {
                var query = _context.ComprobanteCabecera.AsNoTracking()
                    .Include(c => c.TipoDocumentoVenta)
                    .Include(c => c.ComprobanteDetalles).ThenInclude(d => d.Producto)
                    .AsQueryable();

                if (payload.SucursalId.HasValue)
                    query = query.Where(c => c.SucursalId == payload.SucursalId);

                if (DateTime.TryParse(payload.FechaInicio, out var inicio))
                    query = query.Where(c => c.FechaVenta >= inicio.Date);

                if (DateTime.TryParse(payload.FechaFin, out var fin))
                    query = query.Where(c => c.FechaVenta <= fin.Date.AddDays(1).AddTicks(-1));

                var cabeceras = await query.OrderBy(c => c.FechaVenta).ToListAsync();

                var reporte = cabeceras.SelectMany(c => c.ComprobanteDetalles.Select(d => new ReporteDetalladoVentaDto
                {
                    Fecha = c.FechaVenta?.ToString("dd/MM/yyyy") ?? "",
                    SerieCorrelativo = $"{c.Serie}-{c.Correlativo.ToString().PadLeft(7, '0')}",
                    TipoComprobante = c.TipoDocumentoVenta?.Nombre ?? "",
                    Cliente = c.RazonSocial,
                    NumeroDocumento = c.NumeroDocumento,
                    Producto = d.Producto?.Nombre,
                    Cantidad = d.Cantidad,
                    ValorUnitario = d.ValorUnitario,
                    Subtotal = Math.Round(d.ValorUnitarioTotal - d.ValorIgv, 2),
                    Igv = d.ValorIgv,
                    Importe = d.ValorUnitarioTotal
                })).ToList();

                return (ServiceStatus.Ok, reporte, "Success");
            }
            catch (Exception e)
            {
                return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
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
