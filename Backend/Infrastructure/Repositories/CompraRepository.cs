using Application.Abstractions;
using Application.Helper;
using Application.Interfaces.IRepository;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly TaxCalculatorFactory _taxCalculatorFactory;

    public CompraRepository(
        SpaContext context,
        IMapper mapper,
        IHttpContextAccessor? httpContextAccessor,
        TaxCalculatorFactory? taxCalculatorFactory = null)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _taxCalculatorFactory = taxCalculatorFactory ?? new TaxCalculatorFactory();
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    private async Task<string> GenerarNumeroCompra()
    {
        var count = await _context.Compra.CountAsync();
        return $"C-{(count + 1).ToString().PadLeft(6, '0')}";
    }

    // Si ya viene un proveedorId (elegido de la lista existente o editando una compra) se usa tal
    // cual. Si no, y hay Ruc, se empareja por Ruc o se crea -- mismo criterio que ImportarXmlCompra.
    private async Task<int?> ObtenerOCrearProveedorPorRuc(int? proveedorId, string? ruc, string? nombre, string? direccion, string? ubigeoId, string? email)
    {
        if (proveedorId.HasValue) return proveedorId;
        if (string.IsNullOrWhiteSpace(ruc)) return null;

        var proveedorExistente = await _context.Proveedor.AsTracking().FirstOrDefaultAsync(p => p.Ruc == ruc);
        if (proveedorExistente != null) return proveedorExistente.Id;

        if (string.IsNullOrWhiteSpace(nombre)) return null;

        var nuevoProveedor = new Proveedor { Nombre = nombre, Ruc = ruc, Dirección = direccion, UbigeoId = ubigeoId, Email = email };
        await _context.Proveedor.AddAsync(nuevoProveedor);
        await _context.SaveChangesAsync();
        return nuevoProveedor.Id;
    }

    // Subtotal -> Descuento -> Otros cargos -> separacion Gravada/IGV segun el TipoIgv elegido para
    // el documento (a diferencia de las ventas, en compras el IGV es por documento, no por linea).
    private async Task<(decimal montoDescuento, decimal otrosCargos, decimal gravada, decimal igv, decimal total)> CalcularTotalesCompra(
        decimal subtotalProductos, decimal? porcentajeDescuento, decimal? montoDescuentoFijo, decimal? otrosCargosPayload, int? tipoIgvId)
    {
        var montoDescuento = porcentajeDescuento.HasValue && porcentajeDescuento.Value > 0
            ? Math.Round(subtotalProductos * porcentajeDescuento.Value / 100m, 2)
            : montoDescuentoFijo ?? 0m;

        var otrosCargos = otrosCargosPayload ?? 0m;

        var baseConDescuento = subtotalProductos - montoDescuento + otrosCargos;

        var tipoIgv = tipoIgvId.HasValue
            ? await _context.TipoIgv.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tipoIgvId)
            : null;
        var aplicaImpuesto = tipoIgv?.AplicaPorcentajeImpuesto ?? true;

        var config = await _context.ConfiguracionFiscal
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == _context.CurrentTenantName && x.Activo && x.Estado)
            .FirstOrDefaultAsync();
        var calculadora = _taxCalculatorFactory.GetCalculator(PaisIdClaim);

        var gravada = aplicaImpuesto ? calculadora.CalcularSubtotal(baseConDescuento, PaisIdClaim, config?.PorcentajeImpuesto) : baseConDescuento;
        var igv = aplicaImpuesto ? calculadora.CalcularImpuesto(baseConDescuento, PaisIdClaim, config?.PorcentajeImpuesto) : 0m;

        return (montoDescuento, otrosCargos, gravada, igv, baseConDescuento);
    }

    // Suma (+1) o resta (-1) lo facturado en las lineas de la orden y recalcula su estado
    // (RECIBIDA totalmente facturada -> CERRADA; al anular la factura se reabre).
    private async Task AjustarFacturadoOrden(int ordenId, IEnumerable<CompraDetalle> lineas, int signo)
    {
        var orden = await _context.OrdenCompra.AsTracking().Include(o => o.Detalles).FirstAsync(o => o.Id == ordenId);
        foreach (var l in lineas)
        {
            var od = orden.Detalles.FirstOrDefault(d => d.ProductoId == l.ProductoId);
            if (od != null) od.CantidadFacturada = Math.Max(0, od.CantidadFacturada + signo * l.Cantidad);
        }
        OrdenCompraEstado.Recalcular(orden);
    }

    public Task<(ServiceStatus, CompraDto?, string)> CrearCompra(CreateCompraPayload payload)
    {
        payload.OrdenCompraId = null; // solo CrearCompraDeOrden (con su cruce) puede enlazar una orden
        return CrearCompraCore(payload, deOrden: false);
    }

    // Factura de una orden de compra (flujo completo): el stock ya subio en la recepcion, aqui solo
    // se registra el documento y se descuenta lo pendiente de facturar en la orden.
    public Task<(ServiceStatus, CompraDto?, string)> CrearCompraDeOrden(CreateCompraPayload payload)
        => CrearCompraCore(payload, deOrden: true);

    private async Task<(ServiceStatus, CompraDto?, string)> CrearCompraCore(CreateCompraPayload payload, bool deOrden)
    {
        if (payload.Detalle == null || payload.Detalle.Count == 0)
            return (ServiceStatus.FailedValidation, null, "La compra debe incluir al menos un producto");

        await _context.Database.BeginTransactionAsync();

        try
        {
            var subtotalProductos = payload.Detalle.Sum(d => d.Cantidad * d.CostoUnitario);

            var proveedorId = await ObtenerOCrearProveedorPorRuc(
                payload.ProveedorId, payload.ProveedorRuc, payload.ProveedorNombre,
                payload.ProveedorDireccion, payload.ProveedorUbigeoId, payload.ProveedorEmail);

            if (payload.EsCredito && proveedorId == null)
                return (ServiceStatus.FailedValidation, null, "Una compra a crédito necesita un proveedor");

            var (montoDescuento, otrosCargos, gravada, igv, total) = await CalcularTotalesCompra(
                subtotalProductos, payload.PorcentajeDescuento, payload.MontoDescuento, payload.OtrosCargos, payload.TipoIgvId);

            var compra = new Compra
            {
                NumeroCompra = await GenerarNumeroCompra(),
                SucursalId = payload.SucursalId,
                ProveedorId = proveedorId,
                Total = total,
                MetodoPagoId = payload.MetodoPagoId,
                Observacion = payload.Observacion,
                Estado = "CONFIRMADO",
                FechaCompra = payload.FechaCompra ?? NowLocal(),
                Serie = payload.Serie,
                Numero = payload.Numero,
                FechaEmision = payload.FechaEmision,
                MonedaId = payload.MonedaId,
                TipoIgvId = payload.TipoIgvId,
                PorcentajeDescuento = payload.PorcentajeDescuento,
                MontoDescuento = montoDescuento,
                OtrosCargos = otrosCargos,
                EsCredito = payload.EsCredito,
                FechaVencimiento = payload.FechaVencimiento,
                ValorGravada = gravada,
                ValorIgv = igv,
                OrdenCompraId = deOrden ? payload.OrdenCompraId : null,
                StockYaIngresado = deOrden
            };

            await _context.Compra.AddAsync(compra);
            await _context.SaveChangesAsync();

            var detalle = payload.Detalle.Select(d => new CompraDetalle
            {
                CompraId = compra.Id,
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                CostoUnitario = d.CostoUnitario
            }).ToList();

            await _context.CompraDetalle.AddRangeAsync(detalle);
            await _context.SaveChangesAsync();

            foreach (var item in deOrden ? new List<CompraDetalle>() : detalle)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null)
                    return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                var stockAnterior = producto.Stock ?? 0;
                producto.Stock = stockAnterior + item.Cantidad;
                producto.CostoUnitario = item.CostoUnitario;

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = producto.Stock.Value,
                    ReferenciaTipo = "Compra",
                    ReferenciaId = compra.Id
                });
            }

            if (deOrden)
                await AjustarFacturadoOrden(compra.OrdenCompraId!.Value, detalle, +1);

            await _context.SaveChangesAsync();

            await _context.Database.CommitTransactionAsync();

            var (_, dto, _) = await ObtenerCompra(compra.Id);

            return (ServiceStatus.Ok, dto, "Compra registrada correctamente");
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al registrar compra -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, CompraDto?, string)> AnularCompra(int id)
    {
        var compra = await _context.Compra.AsTracking()
                                    .Include(c => c.CompraDetalles)
                                    .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            return (ServiceStatus.NotFound, null, $"No se encontro la compra {id}");

        if (compra.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, null, "La compra ya se encuentra anulada");

        await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var item in compra.StockYaIngresado ? new List<CompraDetalle>() : compra.CompraDetalles)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null) continue;

                var stockAnterior = producto.Stock ?? 0;
                var stockNuevo = stockAnterior - item.Cantidad;

                if (stockNuevo < 0)
                    return (ServiceStatus.FailedValidation, null, $"Stock insuficiente para revertir la compra del producto {producto.Nombre}");

                producto.Stock = stockNuevo;

                // El costo del producto es "última compra registrada" (ver CrearCompra), no un
                // promedio con historial propio: al anular hay que recalcularlo desde la compra
                // vigente más reciente, o se queda con el precio de la compra anulada para siempre.
                producto.CostoUnitario = await _context.CompraDetalle
                    .Where(cd => cd.ProductoId == item.ProductoId
                              && cd.CompraId != compra.Id
                              && cd.Compra.Estado != "ANULADO")
                    .OrderByDescending(cd => cd.Compra.FechaCompra)
                    .ThenByDescending(cd => cd.Id)
                    .Select(cd => (decimal?)cd.CostoUnitario)
                    .FirstOrDefaultAsync();

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = stockNuevo,
                    ReferenciaTipo = "CompraAnulada",
                    ReferenciaId = compra.Id
                });
            }

            compra.Estado = "ANULADO";

            if (compra.OrdenCompraId.HasValue)
                await AjustarFacturadoOrden(compra.OrdenCompraId.Value, compra.CompraDetalles, -1);

            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();

            var (_, dto, _) = await ObtenerCompra(compra.Id);

            return (ServiceStatus.Ok, dto, "Compra anulada correctamente");
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al anular compra -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<CompraDto>?, string)> ListarCompras(CompraQueryParams payload)
    {
        try
        {
            var query = _context.Compra.AsNoTracking().Include(c => c.Sucursal).Include(c => c.Moneda).Include(c => c.TipoIgv).Where(c => c.Estado != "ANULADO").AsQueryable();

            if (payload.ProveedorId.HasValue)
                query = query.Where(c => c.ProveedorId == payload.ProveedorId);

            if (payload.SucursalId.HasValue)
                query = query.Where(c => c.SucursalId == payload.SucursalId);

            if (DateTime.TryParse(payload.StartDate, out var start))
                query = query.Where(c => c.FechaCompra.Date >= start.Date);

            if (DateTime.TryParse(payload.EndDate, out var end))
                query = query.Where(c => c.FechaCompra.Date <= end.Date);

            if (!string.IsNullOrEmpty(payload.Value))
                query = query.Where(c => c.NumeroCompra.Contains(payload.Value) ||
                                        (c.Proveedor != null && c.Proveedor.Nombre.Contains(payload.Value)));

            var lista = await query.OrderByDescending(c => c.Id)
                                   .ProjectTo<CompraDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            if (!lista.HasItems)
                return (ServiceStatus.NotFound, null, "No hay compras para mostrar");

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar compras -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, CompraDto?, string)> ObtenerCompra(int id)
    {
        var dto = await _context.Compra.AsNoTracking()
                                .Include(c => c.Sucursal)
                                .Include(c => c.Proveedor)
                                .Include(c => c.Metodopago)
                                .Include(c => c.Moneda)
                                .Include(c => c.TipoIgv)
                                .Include(c => c.CompraDetalles)
                                    .ThenInclude(d => d.Producto)
                                .ProjectTo<CompraDto>(_mapper.ConfigurationProvider)
                                .FirstOrDefaultAsync(c => c.Id == id);

        if (dto == null)
            return (ServiceStatus.NotFound, null, $"No se encontro la compra {id}");

        return (ServiceStatus.Ok, dto, "Success");
    }

    public async Task<(ServiceStatus, string)> ActualizarFechaCompra(int id, DateTime fecha)
    {
        var compra = await _context.Compra.AsTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            return (ServiceStatus.NotFound, $"No se encontro la compra {id}");

        if (compra.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de una compra anulada");

        compra.FechaCompra = fecha;

        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, "Fecha actualizada correctamente");
    }

    public async Task<(ServiceStatus, CompraDto?, string)> ActualizarCompra(int id, CreateCompraPayload payload)
    {
        if (payload.Detalle == null || payload.Detalle.Count == 0)
            return (ServiceStatus.FailedValidation, null, "La compra debe incluir al menos un producto");

        var compra = await _context.Compra.AsTracking()
                                    .Include(c => c.CompraDetalles)
                                    .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            return (ServiceStatus.NotFound, null, $"No se encontro la compra {id}");

        if (compra.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, null, "No se puede editar una compra anulada");

        if (compra.OrdenCompraId.HasValue)
            return (ServiceStatus.FailedValidation, null, "La factura de una orden de compra no se edita: anúlala y regístrala de nuevo");

        await _context.Database.BeginTransactionAsync();

        try
        {
            // Revertir el stock de los productos de la compra tal como estaba registrada.
            foreach (var item in compra.CompraDetalles)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null) continue;

                var stockAnterior = producto.Stock ?? 0;
                var stockNuevo = stockAnterior - item.Cantidad;

                if (stockNuevo < 0)
                    return (ServiceStatus.FailedValidation, null, $"Stock insuficiente para editar la compra: el producto {producto.Nombre} ya no tiene suficiente stock para revertir la cantidad original");

                producto.Stock = stockNuevo;

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = stockNuevo,
                    ReferenciaTipo = "CompraEditada",
                    ReferenciaId = compra.Id
                });
            }

            _context.CompraDetalle.RemoveRange(compra.CompraDetalles);

            var nuevoDetalle = payload.Detalle.Select(d => new CompraDetalle
            {
                CompraId = compra.Id,
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                CostoUnitario = d.CostoUnitario
            }).ToList();

            var subtotalProductos = nuevoDetalle.Sum(d => d.Cantidad * d.CostoUnitario);

            var proveedorId = await ObtenerOCrearProveedorPorRuc(
                payload.ProveedorId, payload.ProveedorRuc, payload.ProveedorNombre,
                payload.ProveedorDireccion, payload.ProveedorUbigeoId, payload.ProveedorEmail);

            var (montoDescuento, otrosCargos, gravada, igv, total) = await CalcularTotalesCompra(
                subtotalProductos, payload.PorcentajeDescuento, payload.MontoDescuento, payload.OtrosCargos, payload.TipoIgvId);

            compra.SucursalId = payload.SucursalId;
            compra.ProveedorId = proveedorId;
            compra.MetodoPagoId = payload.MetodoPagoId;
            compra.Observacion = payload.Observacion;
            compra.FechaCompra = payload.FechaCompra ?? compra.FechaCompra;
            compra.Serie = payload.Serie;
            compra.Numero = payload.Numero;
            compra.FechaEmision = payload.FechaEmision;
            compra.MonedaId = payload.MonedaId;
            compra.TipoIgvId = payload.TipoIgvId;
            compra.PorcentajeDescuento = payload.PorcentajeDescuento;
            compra.MontoDescuento = montoDescuento;
            compra.OtrosCargos = otrosCargos;
            compra.EsCredito = payload.EsCredito;
            compra.FechaVencimiento = payload.FechaVencimiento;
            compra.ValorGravada = gravada;
            compra.ValorIgv = igv;
            compra.Total = total;

            await _context.CompraDetalle.AddRangeAsync(nuevoDetalle);
            await _context.SaveChangesAsync();

            // Aplicar el stock de los productos con la nueva composición de la compra.
            foreach (var item in nuevoDetalle)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null)
                    return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                var stockAnterior = producto.Stock ?? 0;
                producto.Stock = stockAnterior + item.Cantidad;
                producto.CostoUnitario = item.CostoUnitario;

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = item.Cantidad,
                    StockAnterior = stockAnterior,
                    StockPosterior = producto.Stock.Value,
                    ReferenciaTipo = "CompraEditada",
                    ReferenciaId = compra.Id
                });
            }

            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();

            var (_, dto, _) = await ObtenerCompra(compra.Id);

            return (ServiceStatus.Ok, dto, "Compra actualizada correctamente");
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al editar compra -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, CompraXmlPreviewDto?, string)> ImportarXmlCompra(Stream xmlStream)
    {
        System.Xml.Linq.XDocument doc;
        try
        {
            doc = System.Xml.Linq.XDocument.Load(xmlStream);
        }
        catch (Exception)
        {
            return (ServiceStatus.FailedValidation, null, "El archivo no es un XML válido");
        }

        var root = doc.Root;
        if (root == null)
            return (ServiceStatus.FailedValidation, null, "El XML está vacío");

        System.Xml.Linq.XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        System.Xml.Linq.XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

        var numeroDocumento = root.Element(cbc + "ID")?.Value;

        DateTime? fechaEmision = DateTime.TryParse(root.Element(cbc + "IssueDate")?.Value, out var fecha) ? fecha : null;

        var supplierParty = root.Element(cac + "AccountingSupplierParty")?.Element(cac + "Party");
        var ruc = supplierParty?.Element(cac + "PartyIdentification")?.Element(cbc + "ID")?.Value?.Trim();
        var razonSocial = (supplierParty?.Element(cac + "PartyLegalEntity")?.Element(cbc + "RegistrationName")?.Value
                            ?? supplierParty?.Element(cac + "PartyName")?.Element(cbc + "Name")?.Value)?.Trim();

        decimal.TryParse(
            root.Element(cac + "LegalMonetaryTotal")?.Element(cbc + "PayableAmount")?.Value,
            System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var total);

        var lineas = new List<CompraXmlLineaDto>();
        foreach (var linea in root.Elements(cac + "InvoiceLine"))
        {
            var descripcion = linea.Element(cac + "Item")?.Element(cbc + "Description")?.Value?.Trim() ?? "Producto";

            decimal.TryParse(linea.Element(cbc + "InvoicedQuantity")?.Value,
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cantidad);

            decimal.TryParse(linea.Element(cac + "Price")?.Element(cbc + "PriceAmount")?.Value,
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var precio);

            lineas.Add(new CompraXmlLineaDto { Descripcion = descripcion, Cantidad = cantidad, PrecioUnitario = precio });
        }

        if (string.IsNullOrWhiteSpace(ruc) && lineas.Count == 0)
            return (ServiceStatus.FailedValidation, null, "No se pudo leer el XML: no tiene la estructura de una factura electrónica UBL de SUNAT");

        // Empareja por RUC (identificador confiable, a diferencia del nombre que puede variar
        // de formato entre el emisor del XML y como quedo registrado aqui). Si no existe, se
        // crea el proveedor -- mismo criterio que ya usa CrearComprobante con el Cliente.
        int? proveedorId = null;
        if (!string.IsNullOrWhiteSpace(ruc))
        {
            var proveedorExistente = await _context.Proveedor.AsTracking().FirstOrDefaultAsync(p => p.Ruc == ruc);

            if (proveedorExistente != null)
            {
                proveedorId = proveedorExistente.Id;
                razonSocial ??= proveedorExistente.Nombre;
            }
            else if (!string.IsNullOrWhiteSpace(razonSocial))
            {
                var nuevoProveedor = new Proveedor { Nombre = razonSocial, Ruc = ruc };
                await _context.Proveedor.AddAsync(nuevoProveedor);
                await _context.SaveChangesAsync();
                proveedorId = nuevoProveedor.Id;
            }
        }

        return (ServiceStatus.Ok, new CompraXmlPreviewDto
        {
            ProveedorId = proveedorId,
            ProveedorNombre = razonSocial,
            ProveedorRuc = ruc,
            NumeroDocumento = numeroDocumento,
            FechaEmision = fechaEmision,
            Total = total,
            Lineas = lineas
        }, "XML leído correctamente");
    }

    // Registro de Compras (PLE 8.1) -- una fila por compra registrada.
    public async Task<(ServiceStatus, List<LibroCompraDto>?, string)> ObtenerLibroCompras(ContabilidadQueryParams payload)
    {
        try
        {
            var query = _context.Compra.AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Moneda)
                .Include(c => c.TipoIgv)
                .Where(c => c.Estado != "ANULADO");

            if (payload.SucursalId.HasValue)
                query = query.Where(c => c.SucursalId == payload.SucursalId);

            if (DateTime.TryParse(payload.FechaInicio, out var inicio))
                query = query.Where(c => c.FechaCompra >= inicio.Date);

            if (DateTime.TryParse(payload.FechaFin, out var fin))
                query = query.Where(c => c.FechaCompra <= fin.Date.AddDays(1).AddTicks(-1));

            var compras = await query.OrderBy(c => c.FechaCompra).ToListAsync();

            var aplicaImpuesto = (Compra c) => c.TipoIgv?.AplicaPorcentajeImpuesto ?? true;

            var libro = compras.Select(c => new LibroCompraDto
            {
                Periodo = c.FechaCompra.ToString("yyyyMM"),
                Cuo = c.Id.ToString().PadLeft(12, '0'),
                FechaEmision = (c.FechaEmision ?? c.FechaCompra).ToString("dd/MM/yyyy"),
                TipoComprobante = "01", // Factura de compra (unico documento que este sistema registra como compra)
                Serie = c.Serie ?? "",
                Numero = c.Numero ?? "",
                TipoDocProveedor = SunatCodigos.TipoDocumentoIdentidadPorNumero(c.Proveedor?.Ruc),
                NumeroDocProveedor = c.Proveedor?.Ruc,
                RazonSocial = c.Proveedor?.Nombre,
                BaseImponibleGravada = aplicaImpuesto(c) ? c.ValorGravada : 0,
                ValorAdquisicionesNoGravadas = aplicaImpuesto(c) ? 0 : c.ValorGravada,
                Igv = c.ValorIgv,
                ImporteTotal = c.Total,
                Moneda = c.Moneda?.Codigo ?? "PEN",
                Estado = "1"
            }).ToList();

            return (ServiceStatus.Ok, libro, "Success");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
        }
    }

    // Una fila por producto comprado (no por compra).
    public async Task<(ServiceStatus, List<ReporteDetalladoCompraDto>?, string)> ObtenerReporteDetalladoCompras(ContabilidadQueryParams payload)
    {
        try
        {
            var query = _context.Compra.AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.CompraDetalles).ThenInclude(d => d.Producto)
                .Where(c => c.Estado != "ANULADO");

            if (payload.SucursalId.HasValue)
                query = query.Where(c => c.SucursalId == payload.SucursalId);

            if (DateTime.TryParse(payload.FechaInicio, out var inicio))
                query = query.Where(c => c.FechaCompra >= inicio.Date);

            if (DateTime.TryParse(payload.FechaFin, out var fin))
                query = query.Where(c => c.FechaCompra <= fin.Date.AddDays(1).AddTicks(-1));

            var compras = await query.OrderBy(c => c.FechaCompra).ToListAsync();

            var reporte = compras.SelectMany(c => c.CompraDetalles.Select(d => new ReporteDetalladoCompraDto
            {
                Fecha = c.FechaCompra.ToString("dd/MM/yyyy"),
                SerieNumero = string.IsNullOrEmpty(c.Serie) ? c.NumeroCompra : $"{c.Serie}-{c.Numero}",
                Proveedor = c.Proveedor?.Nombre,
                Ruc = c.Proveedor?.Ruc,
                Producto = d.Producto?.Nombre,
                Cantidad = d.Cantidad,
                CostoUnitario = d.CostoUnitario,
                Subtotal = d.Cantidad * d.CostoUnitario
            })).ToList();

            return (ServiceStatus.Ok, reporte, "Success");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ObtenerSerieNumero(int? sucursalId)
    {
        try
        {
            var config = await _context.ConfiguracionFiscal
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == _context.CurrentTenantName && x.Activo && x.Estado)
                .FirstOrDefaultAsync();

            var serie = config?.SerieCompra ?? "C001";

            // Correlativo por serie (igual criterio que GenerarNumeroCompra, pero acotado a la
            // serie configurada en vez de todas las compras del tenant).
            var correlativo = await _context.Compra.CountAsync(c => c.Serie == serie) + 1;
            var numero = correlativo.ToString().PadLeft(6, '0');

            return (ServiceStatus.Ok, new { serie, numero }, "Success");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error Interno {e.InnerException?.Message ?? e.Message}");
        }
    }
}