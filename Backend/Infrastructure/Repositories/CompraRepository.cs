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
using Infrastructure.Common;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly IAsientoContableRepository _asientoContableRepository;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly TaxCalculatorFactory _taxCalculatorFactory;

    public CompraRepository(
        SpaContext context,
        IMapper mapper,
        IAsientoContableRepository asientoContableRepository,
        IHttpContextAccessor? httpContextAccessor,
        TaxCalculatorFactory? taxCalculatorFactory = null)
    {
        _context = context;
        _mapper = mapper;
        _asientoContableRepository = asientoContableRepository;
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

    // Reparte el debe del asiento entre las cuentas contables que se hayan elegido linea por linea
    // (CompraDetalle.CuentaContableId), agrupando por cuenta y escalando los montos crudos
    // (Cantidad*CostoUnitario, pre-descuento) a la proporcion de "gravada" (post-descuento/otros
    // cargos, pre-IGV) para que la suma cuadre exacto -- el ultimo grupo absorbe el redondeo.
    // cuentaDe(d) da la cuenta elegida para la linea; si no hay, cae en porDefecto(d).
    private async Task<List<LineaAsientoContable>> LineasDebePorCuenta(
        List<CompraDetalle> detalle, decimal subtotalProductos, decimal gravada, Func<CompraDetalle, int?> CuentaDe, Func<CompraDetalle, string> porDefecto)
    {
        var cuentaIds = detalle.Select(CuentaDe).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var codigos = cuentaIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.CuentaContable.AsNoTracking().Where(c => cuentaIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Codigo);

        var factor = subtotalProductos > 0 ? gravada / subtotalProductos : 1m;
        var grupos = detalle
            .GroupBy(d => (Cuenta: CuentaDe(d) is int id && codigos.TryGetValue(id, out var cod) ? cod : porDefecto(d), d.CentroCostoId))
            .Select(g => (g.Key.Cuenta, g.Key.CentroCostoId, Monto: Math.Round(g.Sum(d => d.Cantidad * d.CostoUnitario) * factor, 2)))
            .Where(g => g.Monto != 0)
            .ToList();

        if (grupos.Count == 0) return detalle.Count == 0 ? new() : new List<LineaAsientoContable> { new(porDefecto(detalle[0]), gravada, 0) };

        // Ajusta el ultimo grupo para que la suma cuadre exacto con "gravada" pese al redondeo por grupo.
        var diferencia = gravada - grupos.Sum(g => g.Monto);
        if (diferencia != 0)
        {
            var ultimo = grupos[^1];
            grupos[^1] = (ultimo.Cuenta, ultimo.CentroCostoId, ultimo.Monto + diferencia);
        }

        return grupos.Select(g => new LineaAsientoContable(g.Cuenta, g.Monto, 0) { CentroCosto1Id = g.CentroCostoId }).ToList();
    }

    // Si una linea postea a una cuenta con ModoCentroCosto=OBLIGATORIO, esa linea debe traer su
    // propio CentroCostoId (ver CuentaContable.ModoCentroCosto / "configuracion plan de cuentas
    // ERPdocx.docx": "fundamental para las cuentas de gastos y costos").
    private async Task<string?> ValidarCentroCostoObligatorio(List<CompraDetalle> detalle)
    {
        var cuentaIds = detalle.Where(d => d.CuentaContableId.HasValue).Select(d => d.CuentaContableId!.Value).Distinct().ToList();
        if (cuentaIds.Count == 0) return null;

        var obligatorias = await _context.CuentaContable.AsNoTracking()
            .Where(c => cuentaIds.Contains(c.Id) && c.ModoCentroCosto == ModoCentroCostoCuenta.Obligatorio)
            .ToDictionaryAsync(c => c.Id, c => $"{c.Codigo} - {c.Nombre}");
        if (obligatorias.Count == 0) return null;

        var faltante = detalle.FirstOrDefault(d => d.CuentaContableId.HasValue && obligatorias.ContainsKey(d.CuentaContableId.Value) && d.CentroCostoId == null);
        return faltante == null ? null : $"La cuenta {obligatorias[faltante.CuentaContableId!.Value]} exige centro de costo en cada línea";
    }

    // Suma (+1) o resta (-1) lo facturado en las lineas de la orden y recalcula su estado
    // (RECIBIDA totalmente facturada -> CERRADA; al anular la factura se reabre).
    private async Task AjustarFacturadoOrden(int ordenId, IEnumerable<CompraDetalle> lineas, int signo)
    {
        var orden = await _context.OrdenCompra.AsTracking().Include(o => o.Detalles).FirstAsync(o => o.Id == ordenId);
        foreach (var l in lineas)
        {
            var od = l.OrdenCompraDetalleId.HasValue
                ? orden.Detalles.FirstOrDefault(d => d.Id == l.OrdenCompraDetalleId)
                : orden.Detalles.FirstOrDefault(d => d.ProductoId == l.ProductoId);
            if (od != null) od.CantidadFacturada = Math.Max(0, od.CantidadFacturada + signo * l.Cantidad);
        }
        OrdenCompraEstado.Recalcular(orden);
    }

    // Un mismo documento del proveedor (serie-numero) solo se registra una vez: evita la doble
    // factura por doble clic o por registrarla de nuevo desde otra pantalla.
    private async Task<string?> ValidarDocumentoDuplicado(int? proveedorId, string? serie, string? numero, int? excluirCompraId = null)
    {
        if (string.IsNullOrWhiteSpace(serie) || string.IsNullOrWhiteSpace(numero)) return null;
        var s = serie.Trim().ToUpper();
        var n = numero.Trim();
        var existente = await _context.Compra.AsNoTracking()
            .Where(c => c.Estado != "ANULADO" && c.ProveedorId == proveedorId && c.Id != excluirCompraId
                && c.Serie != null && c.Numero != null && c.Serie.ToUpper() == s && c.Numero == n)
            .Select(c => c.NumeroCompra).FirstOrDefaultAsync();
        return existente == null ? null : $"El documento {s}-{n} de este proveedor ya está registrado en la compra {existente}";
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
        // Fuera de una orden (compra simplificada), toda linea es un bien del catalogo: aun no hay
        // flujo de compra directa de servicios sin orden.
        if (!deOrden && payload.Detalle.Any(d => d.ProductoId == null))
            return (ServiceStatus.FailedValidation, null, "Selecciona un producto del catálogo para cada línea");
        if (payload.Detalle.Any(d => d.ProductoId == null && string.IsNullOrWhiteSpace(d.Descripcion)))
            return (ServiceStatus.FailedValidation, null, "Indica la descripción de cada línea de servicio");

        await _context.Database.BeginTransactionAsync();

        try
        {
            var subtotalProductos = payload.Detalle.Sum(d => d.Cantidad * d.CostoUnitario);

            var proveedorId = await ObtenerOCrearProveedorPorRuc(
                payload.ProveedorId, payload.ProveedorRuc, payload.ProveedorNombre,
                payload.ProveedorDireccion, payload.ProveedorUbigeoId, payload.ProveedorEmail);

            if (payload.EsCredito && proveedorId == null)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.FailedValidation, null, "Una compra a crédito necesita un proveedor");
            }
            if (await ValidarDocumentoDuplicado(proveedorId, payload.Serie, payload.Numero) is { } duplicado)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.FailedValidation, null, duplicado);
            }

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
                TipoCambio = payload.TipoCambio,
                TipoDetraccionId = payload.TipoDetraccionId,
                NumeroDetraccion = payload.NumeroDetraccion,
                FechaDetraccion = payload.FechaDetraccion,
                OrdenCompraId = deOrden ? payload.OrdenCompraId : null,
                StockYaIngresado = deOrden
            };

            await _context.Compra.AddAsync(compra);
            await _context.SaveChangesAsync();

            // Si la linea viene de una orden y no trae su propio Centro de Costo/Cuenta, hereda lo
            // que ya se eligio al crear la orden -- evita pedirselo de nuevo al facturar.
            var ordenDetalleIds = payload.Detalle.Where(d => d.OrdenCompraDetalleId.HasValue && (d.CentroCostoId == null || d.CuentaContableId == null))
                .Select(d => d.OrdenCompraDetalleId!.Value).ToList();
            var clasificacionOrden = ordenDetalleIds.Count == 0
                ? new Dictionary<int, (int? CentroCostoId, int? CuentaContableId)>()
                : await _context.OrdenCompraDetalle.AsNoTracking().Where(od => ordenDetalleIds.Contains(od.Id))
                    .ToDictionaryAsync(od => od.Id, od => (od.CentroCostoId, od.CuentaContableId));

            var detalle = payload.Detalle.Select(d =>
            {
                var heredado = d.OrdenCompraDetalleId.HasValue && clasificacionOrden.TryGetValue(d.OrdenCompraDetalleId.Value, out var c) ? c : default;
                return new CompraDetalle
                {
                    CompraId = compra.Id,
                    ProductoId = d.ProductoId,
                    Descripcion = d.Descripcion,
                    OrdenCompraDetalleId = d.OrdenCompraDetalleId,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.CostoUnitario,
                    CentroCostoId = d.CentroCostoId ?? heredado.CentroCostoId,
                    CuentaContableId = d.CuentaContableId ?? heredado.CuentaContableId
                };
            }).ToList();

            if (await ValidarCentroCostoObligatorio(detalle) is { } errorCentroCosto)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.FailedValidation, null, errorCentroCosto);
            }

            await _context.CompraDetalle.AddRangeAsync(detalle);
            await _context.SaveChangesAsync();

            foreach (var item in deOrden ? new List<CompraDetalle>() : detalle)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null)
                    return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                if (producto.EsServicio) continue; // servicio: sin inventario ni costo promedio

                // El costo (promedio ponderado) es del producto en general, no por sucursal -- se
                // calcula con el stock TOTAL antes de que el ajuste de sucursal lo modifique.
                var stockTotalAnterior = producto.Stock ?? 0;
                producto.CostoUnitario = CosteoInventario.PromedioPonderado(stockTotalAnterior, producto.CostoUnitario ?? 0, item.Cantidad, item.CostoUnitario);

                var ajuste = await StockSucursalHelper.Ajustar(_context, producto, compra.SucursalId, item.Cantidad);
                if (!ajuste.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, ajuste.Error);
                }

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    SucursalId = ajuste.SucursalIdUsada,
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = item.Cantidad,
                    StockAnterior = ajuste.StockAnteriorSucursal,
                    StockPosterior = ajuste.StockPosteriorSucursal,
                    ReferenciaTipo = "Compra",
                    ReferenciaId = compra.Id
                });
            }

            if (deOrden)
                await AjustarFacturadoOrden(compra.OrdenCompraId!.Value, detalle, +1);

            await _context.SaveChangesAsync();

            // Asiento contable de la factura. "42" lleva el TOTAL con IGV -- lo que realmente se le
            // debe al proveedor -- y el IGV se reconoce aparte en 40111 (credito fiscal) en vez de
            // mezclarse con el costo/gasto/puente. Usa gravada (post-descuento, pre-IGV) en vez de
            // subtotalProductos (pre-descuento) para que cuadre con compra.Total/ValorIgv.
            // Metodo PCGE 60/61: la factura va a 60 Compras (bien) / 63 (servicio); la mercaderia
            // entra a 20 contra 61 en un asiento aparte (EntradaCompra aqui, o la Recepcion de la orden).
            if (gravada > 0)
            {
                var ordenServicio = deOrden && await _context.OrdenCompra.AsNoTracking()
                    .Where(o => o.Id == compra.OrdenCompraId).Select(o => o.TipoOrden).FirstOrDefaultAsync() == TipoOrdenCompra.Servicio;

                // Si el proveedor tiene su propia Cuenta por Pagar (configurada al crearlo), la
                // factura postea ahi en vez de "42" -- mismo patron que Producto.CuentaIngresoId.
                var rucProveedor = compra.ProveedorId.HasValue
                    ? await _context.Proveedor.AsNoTracking().Where(p => p.Id == compra.ProveedorId).Select(p => p.Ruc).FirstOrDefaultAsync()
                    : null;
                var cuentaPorPagarCodigo = compra.ProveedorId.HasValue
                    ? await _context.Proveedor.AsNoTracking().Where(p => p.Id == compra.ProveedorId)
                        .Select(p => p.CuentaPorPagarId.HasValue ? p.CuentaPorPagar!.Codigo : null).FirstOrDefaultAsync()
                    : null;

                var productoIds = detalle.Where(d => d.ProductoId.HasValue).Select(d => d.ProductoId!.Value).Distinct().ToList();
                var productosCuenta = await _context.Producto.AsNoTracking().Where(p => productoIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
                Producto? ProductoDe(CompraDetalle d) => d.ProductoId is int pid && productosCuenta.TryGetValue(pid, out var p) ? p : null;
                bool EsServicio(CompraDetalle d) => ProductoDe(d)?.EsServicio ?? ordenServicio;

                // Debe de la factura: cuenta de la linea; si no, la de gasto del servicio; si no, 63/60.
                var lineasFactura = new List<LineaAsientoContable>();
                lineasFactura.AddRange(await LineasDebePorCuenta(detalle, subtotalProductos, gravada,
                    d => d.CuentaContableId ?? (EsServicio(d) ? ProductoDe(d)?.CuentaCostoId : null),
                    d => EsServicio(d) ? "63" : "60"));
                if (igv > 0) lineasFactura.Add(new LineaAsientoContable("40111", igv, 0));
                lineasFactura.Add(new LineaAsientoContable(cuentaPorPagarCodigo ?? "42", 0, total) { CuentaAsociada = rucProveedor });

                var (estadoAsiento, _, mensajeAsiento) = await _asientoContableRepository.Generar(
                    OrigenAsientoContable.Factura, compra.Id, $"Factura {compra.NumeroCompra}", lineasFactura,
                    fechaDocumento: compra.FechaEmision ?? compra.FechaCompra, fechaVencimiento: compra.FechaVencimiento);
                if (estadoAsiento != ServiceStatus.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, $"No se pudo generar el asiento contable -> {mensajeAsiento}");
                }

                // Compra directa: la mercaderia entra en este mismo paso -> 20 (inventario del producto) / 61,
                // al mismo valor (post-descuento, pre-IGV) que se cargo a 60 en la factura.
                var bienes = deOrden ? new List<CompraDetalle>() : detalle.Where(d => !EsServicio(d)).ToList();
                var subtotalBienes = bienes.Sum(d => d.Cantidad * d.CostoUnitario);
                var valorBienes = subtotalProductos > 0 ? Math.Round(subtotalBienes * gravada / subtotalProductos, 2) : 0m;
                if (valorBienes > 0)
                {
                    var lineasEntrada = await LineasDebePorCuenta(bienes, subtotalBienes, valorBienes, d => ProductoDe(d)?.CuentaInventarioCompra, _ => "20");
                    // Haber: contrapartida del movimiento Compra de cada producto (61 por defecto).
                    lineasEntrada.AddRange((await LineasDebePorCuenta(bienes, subtotalBienes, valorBienes, d => ProductoDe(d)?.CuentaContrapartidaCompra, _ => "61"))
                        .Select(l => new LineaAsientoContable(l.CuentaCodigo, 0, l.Debe)));
                    var (estadoEntrada, _, mensajeEntrada) = await _asientoContableRepository.Generar(
                        OrigenAsientoContable.EntradaCompra, compra.Id, $"Entrada de mercadería - Factura {compra.NumeroCompra}", lineasEntrada);
                    if (estadoEntrada != ServiceStatus.Ok)
                    {
                        await _context.Database.RollbackTransactionAsync();
                        return (ServiceStatus.FailedValidation, null, $"No se pudo generar el asiento de entrada -> {mensajeEntrada}");
                    }
                }
            }

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

                if (producto == null || producto.EsServicio) continue; // servicio: sin inventario

                // Le quita a Producto.CostoUnitario (promedio ponderado) la contribucion de esta
                // compra ANTES de bajar el stock -- QuitarDePromedio necesita el stock/costo total
                // tal como estaban con esta compra todavia adentro de la mezcla.
                var stockTotalAnterior = producto.Stock ?? 0;
                producto.CostoUnitario = CosteoInventario.QuitarDePromedio(stockTotalAnterior, producto.CostoUnitario ?? 0, item.Cantidad, item.CostoUnitario);

                var ajuste = await StockSucursalHelper.Ajustar(_context, producto, compra.SucursalId, -item.Cantidad);
                if (!ajuste.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, ajuste.Error);
                }

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    SucursalId = ajuste.SucursalIdUsada,
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                    Cantidad = item.Cantidad,
                    StockAnterior = ajuste.StockAnteriorSucursal,
                    StockPosterior = ajuste.StockPosteriorSucursal,
                    ReferenciaTipo = "CompraAnulada",
                    ReferenciaId = compra.Id
                });
            }

            compra.Estado = "ANULADO";

            if (compra.OrdenCompraId.HasValue)
                await AjustarFacturadoOrden(compra.OrdenCompraId.Value, compra.CompraDetalles, -1);

            await _context.SaveChangesAsync();

            await _asientoContableRepository.Reversar(OrigenAsientoContable.Factura, compra.Id);
            await _asientoContableRepository.Reversar(OrigenAsientoContable.EntradaCompra, compra.Id); // NotFound si fue de orden

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

    // ---------- Notas de credito/debito de compra ----------
    // Espejo de ComprobanteRepository.CrearNotaCreditoDebito (ventas): siempre afecta el monto
    // completo de la Compra (no ajustes parciales linea por linea), reusa el mismo catalogo
    // MotivoNota (TipoDocumentoVentaId 4=Credito/5=Debito) y genera su propio asiento -- no anula
    // ni cambia el Estado de la Compra original, a diferencia de AnularCompra.
    public async Task<(ServiceStatus, NotaCompraDto?, string)> CrearNotaCompra(CrearNotaCompraPayload payload)
    {
        if (payload.Tipo != TipoNotaCompra.Credito && payload.Tipo != TipoNotaCompra.Debito)
            return (ServiceStatus.FailedValidation, null, "Tipo de nota inválido");

        var compra = await _context.Compra.AsTracking().Include(c => c.CompraDetalles).FirstOrDefaultAsync(c => c.Id == payload.CompraId);
        if (compra == null) return (ServiceStatus.NotFound, null, $"No se encontró la compra {payload.CompraId}");
        if (compra.Estado == "ANULADO") return (ServiceStatus.FailedValidation, null, "La compra está anulada");

        var motivo = await _context.MotivoNota.AsNoTracking().FirstOrDefaultAsync(m => m.Id == payload.MotivoNotaId);
        if (motivo == null) return (ServiceStatus.FailedValidation, null, $"No se encontró el motivo {payload.MotivoNotaId}");

        var tipoEsperado = payload.Tipo == TipoNotaCompra.Credito ? (int)TipoComprobante.NotaCredito : (int)TipoComprobante.NotaDebito;
        if (motivo.TipoDocumentoVentaId != tipoEsperado)
            return (ServiceStatus.FailedValidation, null, "El motivo no corresponde al tipo de nota elegido");

        await _context.Database.BeginTransactionAsync();
        try
        {
            var prefijo = payload.Tipo == TipoNotaCompra.Credito ? "NCC" : "NCD";
            var numero = $"{prefijo}-{((await _context.NotaCompra.IgnoreQueryFilters().CountAsync(n => n.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";

            var nota = new NotaCompra
            {
                Numero = numero,
                CompraId = compra.Id,
                Tipo = payload.Tipo,
                MotivoNotaId = motivo.Id,
                Fecha = NowLocal(),
                Monto = compra.Total,
                Observacion = payload.Observacion
            };
            _context.NotaCompra.Add(nota);
            await _context.SaveChangesAsync();

            // Reversa stock solo si es Credito con un motivo que revierte stock, y solo para
            // lineas de bien (una linea de servicio, o una compra cuyo stock ya subio por otra via
            // (StockYaIngresado), no tiene inventario que devolver).
            if (payload.Tipo == TipoNotaCompra.Credito && motivo.RevierteStock && !compra.StockYaIngresado)
            {
                foreach (var item in compra.CompraDetalles.Where(d => d.ProductoId.HasValue))
                {
                    var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);
                    if (producto == null || producto.EsServicio) continue; // servicio: sin inventario

                    var ajusteNota = await StockSucursalHelper.Ajustar(_context, producto, compra.SucursalId, -item.Cantidad);
                    if (!ajusteNota.Ok)
                    {
                        await _context.Database.RollbackTransactionAsync();
                        return (ServiceStatus.FailedValidation, null, ajusteNota.Error);
                    }

                    _context.InventoryMovement.Add(new InventoryMovement
                    {
                        SucursalId = ajusteNota.SucursalIdUsada,
                        ProductoId = producto.Id,
                        TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                        Cantidad = item.Cantidad,
                        StockAnterior = ajusteNota.StockAnteriorSucursal,
                        StockPosterior = ajusteNota.StockPosteriorSucursal,
                        ReferenciaTipo = "NotaCompra",
                        ReferenciaId = nota.Id
                    });
                }
                await _context.SaveChangesAsync();
            }

            var glosa = $"Nota {(payload.Tipo == TipoNotaCompra.Credito ? "crédito" : "débito")} {numero} sobre compra {compra.NumeroCompra}";
            var (estadoAsiento, _, mensajeAsiento) = await _asientoContableRepository.GenerarBasadoEn(
                OrigenAsientoContable.Factura, compra.Id, invertido: payload.Tipo == TipoNotaCompra.Credito,
                OrigenAsientoContable.NotaCompra, nota.Id, glosa);
            if (estadoAsiento != ServiceStatus.Ok)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.FailedValidation, null, $"No se pudo generar el asiento contable -> {mensajeAsiento}");
            }

            // Si la nota devolvio mercaderia, tambien se invierte la entrada (20/61 -> 61/20). NotFound
            // = compra anterior al metodo 60/61, sin asiento de entrada propio: no hay nada que invertir.
            if (payload.Tipo == TipoNotaCompra.Credito && motivo.RevierteStock && !compra.StockYaIngresado)
            {
                var (estadoEntrada, _, mensajeEntrada) = await _asientoContableRepository.GenerarBasadoEn(
                    OrigenAsientoContable.EntradaCompra, compra.Id, invertido: true, OrigenAsientoContable.NotaCompraEntrada, nota.Id, $"{glosa} (devolución de mercadería)");
                if (estadoEntrada is not (ServiceStatus.Ok or ServiceStatus.NotFound))
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, $"No se pudo generar el asiento de devolución -> {mensajeEntrada}");
                }
            }

            await _context.Database.CommitTransactionAsync();
            return await ObtenerNotaCompra(nota.Id);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al crear la nota -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, NotaCompraDto?, string)> AnularNotaCompra(int id)
    {
        var nota = await _context.NotaCompra.AsTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (nota == null) return (ServiceStatus.NotFound, null, "Nota no encontrada");
        if (nota.EstadoNota == EstadoNotaCompra.Anulada) return (ServiceStatus.FailedValidation, null, "La nota ya está anulada");

        await _context.Database.BeginTransactionAsync();
        try
        {
            // Devuelve el stock que la nota hubiera restado (si fue credito con reversion de stock).
            // IgnoreQueryFilters: los movimientos quedaron con la sucursal REAL de la compra, que
            // puede no coincidir con la sucursal ambiental de quien anula (mismo motivo que
            // StockSucursalHelper.Ajustar) -- el filtro por sucursal los esconderia igual que si no
            // existieran, y la nota se marcaria anulada sin devolver el stock.
            var movimientos = await _context.InventoryMovement.IgnoreQueryFilters().AsTracking()
                .Where(m => m.TenantId == _context.CurrentTenantName && m.ReferenciaTipo == "NotaCompra" && m.ReferenciaId == nota.Id).ToListAsync();
            foreach (var mov in movimientos)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == mov.ProductoId);
                if (producto == null || producto.EsServicio) continue; // servicio: sin inventario

                // Se devuelve a la MISMA sucursal donde el movimiento original la habia descontado
                // (mov.SucursalId), no a la sucursal actual de la compra (que pudo cambiar desde
                // entonces si la compra se edito).
                var ajusteAnular = await StockSucursalHelper.Ajustar(_context, producto, mov.SucursalId, mov.Cantidad);
                if (!ajusteAnular.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, ajusteAnular.Error);
                }

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    SucursalId = ajusteAnular.SucursalIdUsada,
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = mov.Cantidad,
                    StockAnterior = ajusteAnular.StockAnteriorSucursal,
                    StockPosterior = ajusteAnular.StockPosteriorSucursal,
                    ReferenciaTipo = "NotaCompraAnulada",
                    ReferenciaId = nota.Id
                });
            }

            nota.EstadoNota = EstadoNotaCompra.Anulada;
            await _context.SaveChangesAsync();

            await _asientoContableRepository.Reversar(OrigenAsientoContable.NotaCompra, nota.Id);
            await _asientoContableRepository.Reversar(OrigenAsientoContable.NotaCompraEntrada, nota.Id); // NotFound si no devolvio mercaderia

            await _context.Database.CommitTransactionAsync();
            return await ObtenerNotaCompra(nota.Id);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al anular la nota -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, List<NotaCompraDto>?, string)> ListarNotasCompra(int compraId)
    {
        var notas = await _context.NotaCompra.AsNoTracking()
            .Include(n => n.Compra).Include(n => n.MotivoNota)
            .Where(n => n.CompraId == compraId)
            .OrderByDescending(n => n.Id)
            .ToListAsync();
        return (ServiceStatus.Ok, notas.Select(ToNotaDto).ToList(), "Success");
    }

    private async Task<(ServiceStatus, NotaCompraDto?, string)> ObtenerNotaCompra(int id)
    {
        var nota = await _context.NotaCompra.AsNoTracking()
            .Include(n => n.Compra).Include(n => n.MotivoNota)
            .FirstOrDefaultAsync(n => n.Id == id);
        return nota == null
            ? (ServiceStatus.NotFound, null, "Nota no encontrada")
            : (ServiceStatus.Ok, ToNotaDto(nota), "Success");
    }

    private static NotaCompraDto ToNotaDto(NotaCompra n) => new()
    {
        Id = n.Id,
        Numero = n.Numero,
        CompraId = n.CompraId,
        NumeroCompra = n.Compra?.NumeroCompra,
        Tipo = n.Tipo,
        MotivoDescripcion = n.MotivoNota?.Descripcion,
        Fecha = n.Fecha.ToString("dd/MM/yyyy HH:mm"),
        Monto = n.Monto,
        EstadoNota = n.EstadoNota,
        Observacion = n.Observacion
    };

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
                                        (c.Serie + "-" + c.Numero).Contains(payload.Value) ||
                                        (c.Proveedor != null && (c.Proveedor.Nombre.Contains(payload.Value) || (c.Proveedor.Ruc != null && c.Proveedor.Ruc.Contains(payload.Value)))));

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
                                .Include(c => c.TipoDetraccion)
                                .Include(c => c.CompraDetalles)
                                    .ThenInclude(d => d.Producto)
                                .Include(c => c.CompraDetalles)
                                    .ThenInclude(d => d.CentroCosto)
                                .Include(c => c.CompraDetalles)
                                    .ThenInclude(d => d.CuentaContable)
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

                if (producto == null || producto.EsServicio) continue; // servicio: sin inventario

                var ajusteRevertir = await StockSucursalHelper.Ajustar(_context, producto, compra.SucursalId, -item.Cantidad);
                if (!ajusteRevertir.Ok)
                    return (ServiceStatus.FailedValidation, null, $"Stock insuficiente para editar la compra: el producto {producto.Nombre} ya no tiene suficiente stock para revertir la cantidad original");

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    SucursalId = ajusteRevertir.SucursalIdUsada,
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                    Cantidad = item.Cantidad,
                    StockAnterior = ajusteRevertir.StockAnteriorSucursal,
                    StockPosterior = ajusteRevertir.StockPosteriorSucursal,
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
                CostoUnitario = d.CostoUnitario,
                CentroCostoId = d.CentroCostoId,
                CuentaContableId = d.CuentaContableId
            }).ToList();

            var subtotalProductos = nuevoDetalle.Sum(d => d.Cantidad * d.CostoUnitario);

            var proveedorId = await ObtenerOCrearProveedorPorRuc(
                payload.ProveedorId, payload.ProveedorRuc, payload.ProveedorNombre,
                payload.ProveedorDireccion, payload.ProveedorUbigeoId, payload.ProveedorEmail);

            if (await ValidarDocumentoDuplicado(proveedorId, payload.Serie, payload.Numero, compra.Id) is { } duplicado)
            {
                await _context.Database.RollbackTransactionAsync();
                return (ServiceStatus.FailedValidation, null, duplicado);
            }

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
            compra.TipoCambio = payload.TipoCambio;
            compra.TipoDetraccionId = payload.TipoDetraccionId;
            compra.NumeroDetraccion = payload.NumeroDetraccion;
            compra.FechaDetraccion = payload.FechaDetraccion;

            await _context.CompraDetalle.AddRangeAsync(nuevoDetalle);
            await _context.SaveChangesAsync();

            // Aplicar el stock de los productos con la nueva composición de la compra.
            foreach (var item in nuevoDetalle)
            {
                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null)
                    return (ServiceStatus.FailedValidation, null, $"No se encontro el producto {item.ProductoId}");

                if (producto.EsServicio) continue; // servicio: sin inventario ni costo promedio

                var stockTotalAnterior = producto.Stock ?? 0;
                producto.CostoUnitario = CosteoInventario.PromedioPonderado(stockTotalAnterior, producto.CostoUnitario ?? 0, item.Cantidad, item.CostoUnitario);

                var ajusteNuevo = await StockSucursalHelper.Ajustar(_context, producto, compra.SucursalId, item.Cantidad);
                if (!ajusteNuevo.Ok)
                    return (ServiceStatus.FailedValidation, null, ajusteNuevo.Error);

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    SucursalId = ajusteNuevo.SucursalIdUsada,
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = item.Cantidad,
                    StockAnterior = ajusteNuevo.StockAnteriorSucursal,
                    StockPosterior = ajusteNuevo.StockPosteriorSucursal,
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