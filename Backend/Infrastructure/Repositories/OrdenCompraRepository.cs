using Application.Helper;
using Application.Interfaces.IRepository;
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

// Flujo de compras completo: Orden de compra -> Recepcion (sube el stock) -> Factura del proveedor
// con cruce. Solo aplica cuando ConfiguracionFlujo.FlujoCompras = COMPLETO; en modo simplificado
// Compras funciona como siempre (CompraRepository.CrearCompra).
public class OrdenCompraRepository : IOrdenCompraRepository
{
    private readonly SpaContext _context;
    private readonly ICompraRepository _compraRepository;
    private readonly IDepartamentoRepository _departamentoRepository;
    private readonly IAsientoContableRepository _asientoContableRepository;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public OrdenCompraRepository(SpaContext context, ICompraRepository compraRepository,
        IDepartamentoRepository departamentoRepository, IAsientoContableRepository asientoContableRepository,
        IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _compraRepository = compraRepository;
        _departamentoRepository = departamentoRepository;
        _asientoContableRepository = asientoContableRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    private string? CallerId => _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool CallerEsAdministrador =>
        _httpContextAccessor?.HttpContext?.User.IsInRole("SuperAdmin") == true
        || _httpContextAccessor?.HttpContext?.User.IsInRole("Administrador") == true;

    // ---------- Configuracion ----------

    private async Task<ConfiguracionFlujo?> LeerConfiguracion(bool tracking = false)
    {
        var q = tracking ? _context.ConfiguracionFlujo.AsTracking() : _context.ConfiguracionFlujo.AsNoTracking();
        return await q.OrderBy(c => c.Id).FirstOrDefaultAsync();
    }

    private static ConfiguracionFlujoDto ToDto(ConfiguracionFlujo? c) => new()
    {
        FlujoCompras = c?.FlujoCompras ?? FlujoComprasModo.Simplificado,
        CruceFactura = c?.CruceFactura ?? CruceFacturaModo.Advertir,
        MontoAprobacionOc = c?.MontoAprobacionOc,
        FlujoVentas = c?.FlujoVentas ?? FlujoComprasModo.Simplificado
    };

    public async Task<(ServiceStatus, ConfiguracionFlujoDto?, string)> ObtenerConfiguracion()
        => (ServiceStatus.Ok, ToDto(await LeerConfiguracion()), "Success");

    public async Task<(ServiceStatus, ConfiguracionFlujoDto?, string)> GuardarConfiguracion(ConfiguracionFlujoPayload payload)
    {
        if (payload.FlujoCompras is not (FlujoComprasModo.Simplificado or FlujoComprasModo.Completo))
            return (ServiceStatus.FailedValidation, null, "Flujo de compras no válido");
        if (payload.CruceFactura is not (CruceFacturaModo.Advertir or CruceFacturaModo.Bloquear))
            return (ServiceStatus.FailedValidation, null, "Modo de cruce no válido");
        if (payload.FlujoVentas is not null and not (FlujoComprasModo.Simplificado or FlujoComprasModo.Completo))
            return (ServiceStatus.FailedValidation, null, "Flujo de ventas no válido");
        if (payload.MontoAprobacionOc is < 0)
            return (ServiceStatus.FailedValidation, null, "El monto de aprobación no puede ser negativo");

        var config = await LeerConfiguracion(tracking: true);
        if (config == null)
        {
            config = new ConfiguracionFlujo();
            _context.ConfiguracionFlujo.Add(config);
        }

        config.FlujoCompras = payload.FlujoCompras;
        config.CruceFactura = payload.CruceFactura;
        config.MontoAprobacionOc = payload.MontoAprobacionOc;
        if (payload.FlujoVentas != null) config.FlujoVentas = payload.FlujoVentas;
        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, ToDto(config), "Configuración guardada");
    }

    private async Task<string?> ValidarFlujoCompleto()
    {
        var config = await LeerConfiguracion();
        return (config?.FlujoCompras ?? FlujoComprasModo.Simplificado) == FlujoComprasModo.Completo
            ? null
            : "El flujo completo de compras no está activo (Configuración → Flujo de compras)";
    }

    // ---------- Ordenes ----------

    private async Task<string> GenerarNumero()
        => $"OC-{((await _context.OrdenCompra.IgnoreQueryFilters().CountAsync(o => o.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";

    private async Task<string> GenerarNumeroRecepcion()
        => $"REC-{((await _context.Recepcion.IgnoreQueryFilters().CountAsync(r => r.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";

    private static string? ValidarDetalle(List<OrdenCompraDetallePayload>? detalle, string tipoOrden)
    {
        if (detalle == null || detalle.Count == 0)
            return "La orden debe incluir al menos un producto";
        if (detalle.Any(d => d.Cantidad <= 0))
            return "Las cantidades deben ser mayores a cero";
        if (detalle.Any(d => d.CostoUnitario < 0))
            return "El costo no puede ser negativo";
        if (tipoOrden == TipoOrdenCompra.Servicio)
        {
            if (detalle.Any(d => string.IsNullOrWhiteSpace(d.Descripcion)))
                return "Indica la descripción de cada línea de servicio";
        }
        else
        {
            if (detalle.Any(d => d.ProductoId == null))
                return "Elige un producto del catálogo para cada línea";
            if (detalle.GroupBy(d => d.ProductoId).Any(g => g.Count() > 1))
                return "Un producto no puede repetirse en la misma orden";
        }
        return null;
    }

    // Servicio no tiene bien fisico: al quedar EMITIDA, sus lineas se marcan recibidas de una vez
    // (nunca pasa por Recepcion ni genera InventoryMovement) para que el cruce de factura funcione igual.
    private static void ProcesarServicioSiEmitida(OrdenCompra orden)
    {
        if (orden.TipoOrden != TipoOrdenCompra.Servicio || orden.EstadoOrden != EstadoOrdenCompra.Emitida) return;
        foreach (var d in orden.Detalles) d.CantidadRecibida = d.CantidadPedida;
        OrdenCompraEstado.Recalcular(orden);
    }

    // Segun el monto: sin umbral o total <= umbral -> EMITIDA directo. Si lo supera, hace falta
    // el departamento (default: el del usuario que emite, si tiene uno) y UN aprobador de esa
    // jefatura -> PENDIENTE_APROBACION asignada solo a ese usuario.
    private async Task<(string? Error, string Estado, int? DepartamentoId, string? AprobadorAsignadoId)> ResolverAprobacion(
        decimal total, int? departamentoIdPayload, string? aprobadorIdPayload)
    {
        var config = await LeerConfiguracion();
        if (config?.MontoAprobacionOc is not { } umbral || total <= umbral)
            return (null, EstadoOrdenCompra.Emitida, null, null);

        var departamentoId = departamentoIdPayload;
        if (departamentoId == null && CallerId != null)
            departamentoId = await _context.Users.AsNoTracking()
                .Where(u => u.Id == CallerId).Select(u => u.DepartamentoId).FirstOrDefaultAsync();

        if (departamentoId == null)
            return ("Esta orden supera el monto de aprobación: elige el departamento que debe aprobarla", EstadoOrdenCompra.PendienteAprobacion, null, null);
        if (string.IsNullOrWhiteSpace(aprobadorIdPayload))
            return ("Elige un aprobador del departamento", EstadoOrdenCompra.PendienteAprobacion, departamentoId, null);
        if (!await _departamentoRepository.EsAprobadorDelDepartamento(departamentoId.Value, aprobadorIdPayload))
            return ("El usuario elegido no es aprobador de ese departamento", EstadoOrdenCompra.PendienteAprobacion, departamentoId, null);

        return (null, EstadoOrdenCompra.PendienteAprobacion, departamentoId, aprobadorIdPayload);
    }

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> CrearOrden(CreateOrdenCompraPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (payload.TipoOrden is not (TipoOrdenCompra.Bien or TipoOrdenCompra.Servicio))
            return (ServiceStatus.FailedValidation, null, "Tipo de orden no válido");
        if (ValidarDetalle(payload.Detalle, payload.TipoOrden) is { } error) return (ServiceStatus.FailedValidation, null, error);

        try
        {
            var total = payload.Detalle.Sum(d => d.Cantidad * d.CostoUnitario);
            string estadoOrden = EstadoOrdenCompra.Borrador;
            int? departamentoId = null;
            string? aprobadorId = null;
            if (!payload.Borrador)
            {
                var r = await ResolverAprobacion(total, payload.DepartamentoId, payload.AprobadorAsignadoId);
                if (r.Error != null) return (ServiceStatus.FailedValidation, null, r.Error);
                (estadoOrden, departamentoId, aprobadorId) = (r.Estado, r.DepartamentoId, r.AprobadorAsignadoId);
            }

            var orden = new OrdenCompra
            {
                Numero = await GenerarNumero(),
                SucursalId = payload.SucursalId,
                ProveedorId = payload.ProveedorId,
                MonedaId = payload.MonedaId,
                FechaEmision = NowLocal(),
                Total = total,
                TipoOrden = payload.TipoOrden,
                Observacion = payload.Observacion,
                EstadoOrden = estadoOrden,
                DepartamentoId = departamentoId,
                AprobadorAsignadoId = aprobadorId,
                Detalles = payload.Detalle.Select(d => new OrdenCompraDetalle
                {
                    ProductoId = d.ProductoId,
                    Descripcion = d.Descripcion,
                    CantidadPedida = d.Cantidad,
                    CostoUnitario = d.CostoUnitario,
                    CentroCostoId = d.CentroCostoId,
                    CuentaContableId = d.CuentaContableId
                }).ToList()
            };
            ProcesarServicioSiEmitida(orden);

            _context.OrdenCompra.Add(orden);
            await _context.SaveChangesAsync();

            return await ObtenerOrden(orden.Id);
        }
        catch (Exception e)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al crear orden -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> ActualizarOrden(int id, CreateOrdenCompraPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (payload.TipoOrden is not (TipoOrdenCompra.Bien or TipoOrdenCompra.Servicio))
            return (ServiceStatus.FailedValidation, null, "Tipo de orden no válido");
        if (ValidarDetalle(payload.Detalle, payload.TipoOrden) is { } error) return (ServiceStatus.FailedValidation, null, error);

        var orden = await _context.OrdenCompra.AsTracking().Include(o => o.Detalles).FirstOrDefaultAsync(o => o.Id == id);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.EstadoOrden != EstadoOrdenCompra.Borrador)
            return (ServiceStatus.FailedValidation, null, "Solo se puede editar una orden en borrador");

        _context.OrdenCompraDetalle.RemoveRange(orden.Detalles);
        orden.TipoOrden = payload.TipoOrden;
        orden.Detalles = payload.Detalle.Select(d => new OrdenCompraDetalle
        {
            ProductoId = d.ProductoId,
            Descripcion = d.Descripcion,
            CantidadPedida = d.Cantidad,
            CostoUnitario = d.CostoUnitario,
            CentroCostoId = d.CentroCostoId,
            CuentaContableId = d.CuentaContableId
        }).ToList();
        orden.SucursalId = payload.SucursalId;
        orden.ProveedorId = payload.ProveedorId;
        orden.MonedaId = payload.MonedaId;
        orden.Observacion = payload.Observacion;
        orden.Total = payload.Detalle.Sum(d => d.Cantidad * d.CostoUnitario);
        if (!payload.Borrador)
        {
            var r = await ResolverAprobacion(orden.Total, payload.DepartamentoId, payload.AprobadorAsignadoId);
            if (r.Error != null) return (ServiceStatus.FailedValidation, null, r.Error);
            orden.EstadoOrden = r.Estado;
            orden.DepartamentoId = r.DepartamentoId;
            orden.AprobadorAsignadoId = r.AprobadorAsignadoId;
            ProcesarServicioSiEmitida(orden);
        }

        await _context.SaveChangesAsync();
        return await ObtenerOrden(id);
    }

    private IQueryable<OrdenCompra> QueryOrdenes() => _context.OrdenCompra.AsNoTracking()
        .Include(o => o.Sucursal).Include(o => o.Proveedor)
        .Include(o => o.Departamento).Include(o => o.AprobadorAsignado)
        .Include(o => o.Detalles).ThenInclude(d => d.Producto)
        .Include(o => o.Detalles).ThenInclude(d => d.CentroCosto)
        .Include(o => o.Detalles).ThenInclude(d => d.CuentaContable)
        .Include(o => o.Recepciones);

    private static OrdenCompraDto ToDto(OrdenCompra o) => new()
    {
        Id = o.Id,
        Numero = o.Numero,
        SucursalId = o.SucursalId,
        Sucursal = o.Sucursal?.Nombre,
        ProveedorId = o.ProveedorId,
        Proveedor = o.Proveedor?.Nombre,
        ProveedorRuc = o.Proveedor?.Ruc,
        ProveedorDireccion = o.Proveedor?.Dirección,
        MonedaId = o.MonedaId,
        FechaEmision = o.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
        Total = o.Total,
        EstadoOrden = o.EstadoOrden,
        TipoOrden = o.TipoOrden,
        Observacion = o.Observacion,
        AprobadoPor = o.AprobadoPor,
        MotivoCierre = o.MotivoCierre,
        Usuario = o.UsuarioCreacion,
        DepartamentoId = o.DepartamentoId,
        Departamento = o.Departamento?.Nombre,
        AprobadorAsignadoId = o.AprobadorAsignadoId,
        AprobadorAsignado = o.AprobadorAsignado != null ? $"{o.AprobadorAsignado.FirstName} {o.AprobadorAsignado.LastName}".Trim() : null,
        Detalle = o.Detalles.Select(d => new OrdenCompraDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            Producto = d.Producto?.Nombre,
            Descripcion = d.Descripcion,
            CantidadPedida = d.CantidadPedida,
            CantidadRecibida = d.CantidadRecibida,
            CantidadFacturada = d.CantidadFacturada,
            CostoUnitario = d.CostoUnitario,
            CentroCostoId = d.CentroCostoId,
            CentroCosto = d.CentroCosto?.Nombre,
            CuentaContableId = d.CuentaContableId,
            CuentaContable = d.CuentaContable != null ? $"{d.CuentaContable.Codigo} - {d.CuentaContable.Nombre}" : null
        }).ToList(),
        Recepciones = o.Recepciones.OrderBy(r => r.Id).Select(r => new RecepcionDto
        {
            Id = r.Id,
            Numero = r.Numero,
            Fecha = r.Fecha.ToString("dd/MM/yyyy HH:mm"),
            EstadoRecepcion = r.EstadoRecepcion,
            Observacion = r.Observacion,
            NumeroGuiaRemision = r.NumeroGuiaRemision
        }).ToList()
    };

    public async Task<(ServiceStatus, DataCollection<OrdenCompraDto>?, string)> ListarOrdenes(OrdenCompraQueryParams payload)
    {
        try
        {
            var query = QueryOrdenes().Where(o => o.Estado);
            if (!string.IsNullOrWhiteSpace(payload.Estado))
                query = query.Where(o => o.EstadoOrden == payload.Estado);

            var total = await query.CountAsync();
            var page = Math.Max(1, payload.Page);
            var amount = Math.Max(1, payload.Amount);
            var ordenes = await query.OrderByDescending(o => o.Id).Skip((page - 1) * amount).Take(amount).ToListAsync();

            return (ServiceStatus.Ok, new DataCollection<OrdenCompraDto>
            {
                Items = ordenes.Select(ToDto).ToList(),
                Total = total,
                Page = page,
                Pages = (int)Math.Ceiling(total / (double)amount)
            }, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar órdenes -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> ObtenerOrden(int id)
    {
        var orden = await QueryOrdenes().FirstOrDefaultAsync(o => o.Id == id);
        return orden == null
            ? (ServiceStatus.NotFound, null, "Orden no encontrada")
            : (ServiceStatus.Ok, ToDto(orden), "Success");
    }

    private Task<OrdenCompra?> CargarTracking(int id)
        => _context.OrdenCompra.AsTracking().Include(o => o.Detalles).Include(o => o.Recepciones).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> EmitirOrden(int id, EmitirOrdenCompraPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var orden = await CargarTracking(id);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.EstadoOrden != EstadoOrdenCompra.Borrador)
            return (ServiceStatus.FailedValidation, null, "Solo se emite una orden en borrador");

        var r = await ResolverAprobacion(orden.Total, payload.DepartamentoId, payload.AprobadorAsignadoId);
        if (r.Error != null) return (ServiceStatus.FailedValidation, null, r.Error);
        orden.EstadoOrden = r.Estado;
        orden.DepartamentoId = r.DepartamentoId;
        orden.AprobadorAsignadoId = r.AprobadorAsignadoId;
        ProcesarServicioSiEmitida(orden);
        await _context.SaveChangesAsync();
        return await ObtenerOrden(id);
    }

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> AprobarOrden(int id, string? usuario)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var orden = await CargarTracking(id);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.EstadoOrden != EstadoOrdenCompra.PendienteAprobacion)
            return (ServiceStatus.FailedValidation, null, "La orden no está pendiente de aprobación");
        // Solo el aprobador elegido de la jefatura (o un administrador, como respaldo) puede aprobarla.
        if (!CallerEsAdministrador && (CallerId == null || CallerId != orden.AprobadorAsignadoId))
            return (ServiceStatus.FailedValidation, null, "No eres el aprobador asignado a esta orden");

        orden.EstadoOrden = EstadoOrdenCompra.Emitida;
        orden.AprobadoPor = usuario;
        orden.FechaAprobacion = NowLocal();
        ProcesarServicioSiEmitida(orden);
        await _context.SaveChangesAsync();
        return await ObtenerOrden(id);
    }

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> AnularOrden(int id)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var orden = await CargarTracking(id);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.EstadoOrden is EstadoOrdenCompra.Anulada or EstadoOrdenCompra.Cerrada)
            return (ServiceStatus.FailedValidation, null, "La orden ya está anulada o cerrada");
        if (orden.Recepciones.Any(r => r.EstadoRecepcion == "ACTIVA"))
            return (ServiceStatus.FailedValidation, null, "Anula primero las recepciones de la orden");
        if (orden.Detalles.Any(d => d.CantidadFacturada > 0))
            return (ServiceStatus.FailedValidation, null, "La orden tiene facturas registradas: anúlalas primero");

        orden.EstadoOrden = EstadoOrdenCompra.Anulada;
        await _context.SaveChangesAsync();
        return await ObtenerOrden(id);
    }

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> CerrarOrden(int id, string? motivo)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (string.IsNullOrWhiteSpace(motivo))
            return (ServiceStatus.FailedValidation, null, "Indica el motivo del cierre");
        var orden = await CargarTracking(id);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.EstadoOrden is not (EstadoOrdenCompra.Emitida or EstadoOrdenCompra.RecibidaParcial or EstadoOrdenCompra.Recibida))
            return (ServiceStatus.FailedValidation, null, "La orden no se puede cerrar en su estado actual");

        orden.MotivoCierre = motivo.Trim();
        orden.EstadoOrden = EstadoOrdenCompra.Cerrada;
        await _context.SaveChangesAsync();
        return await ObtenerOrden(id);
    }

    // ---------- Recepcion ----------

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> RegistrarRecepcion(int ordenId, CreateRecepcionPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var lineas = payload.Detalle?.Where(d => d.Cantidad > 0).ToList() ?? new();
        if (lineas.Count == 0) return (ServiceStatus.FailedValidation, null, "Indica al menos una cantidad recibida");
        if (payload.Detalle!.Any(d => d.Cantidad < 0)) return (ServiceStatus.FailedValidation, null, "Las cantidades no pueden ser negativas");

        var orden = await CargarTracking(ordenId);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.TipoOrden == TipoOrdenCompra.Servicio)
            return (ServiceStatus.FailedValidation, null, "Una orden de servicio no requiere recepción");
        if (orden.EstadoOrden is not (EstadoOrdenCompra.Emitida or EstadoOrdenCompra.RecibidaParcial))
            return (ServiceStatus.FailedValidation, null, "Solo se recibe una orden emitida o con recepción parcial");

        foreach (var l in lineas)
        {
            var od = orden.Detalles.FirstOrDefault(d => d.Id == l.OrdenCompraDetalleId);
            if (od == null) return (ServiceStatus.FailedValidation, null, "Una línea no pertenece a la orden");
            if (l.Cantidad > od.CantidadPedida - od.CantidadRecibida)
                return (ServiceStatus.FailedValidation, null, $"No puedes recibir más de lo pendiente ({od.CantidadPedida - od.CantidadRecibida}) de la línea {od.Id}");
        }

        await _context.Database.BeginTransactionAsync();
        try
        {
            var recepcion = new Recepcion
            {
                Numero = await GenerarNumeroRecepcion(),
                SucursalId = orden.SucursalId,
                OrdenCompraId = orden.Id,
                Fecha = NowLocal(),
                Observacion = payload.Observacion,
                NumeroGuiaRemision = payload.NumeroGuiaRemision
            };
            _context.Recepcion.Add(recepcion);
            await _context.SaveChangesAsync();

            var totalRecepcion = 0m;
            var debePorCuentaId = new Dictionary<int, decimal>(); // inventario propio del producto (movimiento Compra)
            var haberPorCuentaId = new Dictionary<int, decimal>(); // contrapartida propia del producto (movimiento Compra)
            foreach (var l in lineas)
            {
                var od = orden.Detalles.First(d => d.Id == l.OrdenCompraDetalleId);
                od.CantidadRecibida += l.Cantidad;
                totalRecepcion += l.Cantidad * od.CostoUnitario;

                _context.RecepcionDetalle.Add(new RecepcionDetalle
                {
                    RecepcionId = recepcion.Id,
                    OrdenCompraDetalleId = od.Id,
                    ProductoId = od.ProductoId!.Value, // solo BIEN llega aqui (RegistrarRecepcion rechaza SERVICIO)
                    Cantidad = l.Cantidad
                });

                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == od.ProductoId);
                if (producto == null)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, $"No se encontró el producto {od.ProductoId}");
                }

                if (producto.CuentaInventarioCompra is int cuentaInventarioId)
                    debePorCuentaId[cuentaInventarioId] = debePorCuentaId.GetValueOrDefault(cuentaInventarioId) + l.Cantidad * od.CostoUnitario;
                if (producto.CuentaContrapartidaCompra is int cuentaContraId)
                    haberPorCuentaId[cuentaContraId] = haberPorCuentaId.GetValueOrDefault(cuentaContraId) + l.Cantidad * od.CostoUnitario;

                if (producto.EsServicio) continue; // servicio: sin inventario

                var stockTotalAnterior = producto.Stock ?? 0;
                producto.CostoUnitario = CosteoInventario.PromedioPonderado(stockTotalAnterior, producto.CostoUnitario ?? 0, l.Cantidad, od.CostoUnitario);

                var ajusteRecepcion = await StockSucursalHelper.Ajustar(_context, producto, recepcion.SucursalId, l.Cantidad);
                if (!ajusteRecepcion.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, ajusteRecepcion.Error);
                }

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    SucursalId = ajusteRecepcion.SucursalIdUsada,
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.Compra,
                    Cantidad = l.Cantidad,
                    StockAnterior = ajusteRecepcion.StockAnteriorSucursal,
                    StockPosterior = ajusteRecepcion.StockPosteriorSucursal,
                    ReferenciaTipo = "Recepcion",
                    ReferenciaId = recepcion.Id
                });
            }

            OrdenCompraEstado.Recalcular(orden);
            await _context.SaveChangesAsync();

            if (totalRecepcion > 0)
            {
                // Debe: cuenta de inventario de cada producto (o "20" por defecto), agrupada.
                var codigos = debePorCuentaId.Count == 0 ? new Dictionary<int, string>()
                    : await _context.CuentaContable.AsNoTracking().Where(c => debePorCuentaId.Keys.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Codigo);
                var debePorCodigo = new Dictionary<string, decimal>();
                foreach (var (cuentaId, monto) in debePorCuentaId)
                    if (codigos.TryGetValue(cuentaId, out var codigo)) debePorCodigo[codigo] = debePorCodigo.GetValueOrDefault(codigo) + monto;
                var restoDefecto = totalRecepcion - debePorCodigo.Values.Sum();
                if (restoDefecto != 0) debePorCodigo["20"] = debePorCodigo.GetValueOrDefault("20") + restoDefecto;

                var lineasRecepcion = debePorCodigo.Select(kv => new LineaAsientoContable(kv.Key, Math.Round(kv.Value, 2), 0)).ToList();
                // Haber: contrapartida del movimiento Compra de cada producto, o "61" (PCGE 60/61: la factura de la orden va a 60).
                var codigosHaber = haberPorCuentaId.Count == 0 ? new Dictionary<int, string>()
                    : await _context.CuentaContable.AsNoTracking().Where(c => haberPorCuentaId.Keys.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Codigo);
                var haberPorCodigo = new Dictionary<string, decimal>();
                foreach (var (cuentaId, monto) in haberPorCuentaId)
                    if (codigosHaber.TryGetValue(cuentaId, out var codigo)) haberPorCodigo[codigo] = haberPorCodigo.GetValueOrDefault(codigo) + Math.Round(monto, 2);
                var totalDebe = lineasRecepcion.Sum(x => x.Debe);
                var restoHaber = totalDebe - haberPorCodigo.Values.Sum();
                if (restoHaber != 0) haberPorCodigo["61"] = haberPorCodigo.GetValueOrDefault("61") + restoHaber;
                lineasRecepcion.AddRange(haberPorCodigo.Select(kv => new LineaAsientoContable(kv.Key, 0, kv.Value)));
                var (estadoAsiento, _, mensajeAsiento) = await _asientoContableRepository.Generar(
                    OrigenAsientoContable.MovimientoInventario, recepcion.Id,
                    $"Recepción {recepcion.Numero} de orden {orden.Numero}", lineasRecepcion);
                if (estadoAsiento != ServiceStatus.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, $"No se pudo generar el asiento contable -> {mensajeAsiento}");
                }
            }

            await _context.Database.CommitTransactionAsync();
            return await ObtenerOrden(ordenId);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al registrar recepción -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, OrdenCompraDto?, string)> AnularRecepcion(int recepcionId)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        var recepcion = await _context.Recepcion.AsTracking().Include(r => r.Detalles).FirstOrDefaultAsync(r => r.Id == recepcionId);
        if (recepcion == null) return (ServiceStatus.NotFound, null, "Recepción no encontrada");
        if (recepcion.EstadoRecepcion != "ACTIVA")
            return (ServiceStatus.FailedValidation, null, "La recepción ya está anulada");

        var orden = await CargarTracking(recepcion.OrdenCompraId);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.EstadoOrden == EstadoOrdenCompra.Anulada)
            return (ServiceStatus.FailedValidation, null, "La orden está anulada");

        foreach (var d in recepcion.Detalles)
        {
            var od = orden.Detalles.First(x => x.Id == d.OrdenCompraDetalleId);
            if (od.CantidadRecibida - d.Cantidad < od.CantidadFacturada)
                return (ServiceStatus.FailedValidation, null, "Hay facturas registradas sobre esta mercadería: anúlalas primero");
        }

        await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var d in recepcion.Detalles)
            {
                var od = orden.Detalles.First(x => x.Id == d.OrdenCompraDetalleId);
                od.CantidadRecibida -= d.Cantidad;

                var producto = await _context.Producto.AsTracking().FirstOrDefaultAsync(p => p.Id == d.ProductoId);
                if (producto == null || producto.EsServicio) continue;

                var ajusteAnularRecepcion = await StockSucursalHelper.Ajustar(_context, producto, recepcion.SucursalId, -d.Cantidad);
                if (!ajusteAnularRecepcion.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    return (ServiceStatus.FailedValidation, null, ajusteAnularRecepcion.Error);
                }

                _context.InventoryMovement.Add(new InventoryMovement
                {
                    SucursalId = ajusteAnularRecepcion.SucursalIdUsada,
                    ProductoId = producto.Id,
                    TipoMovimiento = (int)TipoMovimientoInventario.DevolucionCompra,
                    Cantidad = d.Cantidad,
                    StockAnterior = ajusteAnularRecepcion.StockAnteriorSucursal,
                    StockPosterior = ajusteAnularRecepcion.StockPosteriorSucursal,
                    ReferenciaTipo = "RecepcionAnulada",
                    ReferenciaId = recepcion.Id
                });
            }

            recepcion.EstadoRecepcion = "ANULADA";
            // Si estaba cerrada a mano, al anular una recepcion se reabre para recalcular.
            orden.MotivoCierre = null;
            if (orden.EstadoOrden == EstadoOrdenCompra.Cerrada) orden.EstadoOrden = EstadoOrdenCompra.Recibida;
            OrdenCompraEstado.Recalcular(orden);
            await _context.SaveChangesAsync();

            // Best-effort: si la recepcion no generó asiento (ej. costo total 0), no hay nada que reversar.
            await _asientoContableRepository.Reversar(OrigenAsientoContable.MovimientoInventario, recepcion.Id);

            await _context.Database.CommitTransactionAsync();
            return await ObtenerOrden(orden.Id);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al anular recepción -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    // ---------- Factura con cruce ----------

    public async Task<(ServiceStatus, CompraDto?, string)> FacturarOrden(int ordenId, FacturarOrdenCompraPayload payload)
    {
        if (await ValidarFlujoCompleto() is { } noActivo) return (ServiceStatus.FailedValidation, null, noActivo);
        if (payload.Detalle == null || payload.Detalle.Count == 0)
            return (ServiceStatus.FailedValidation, null, "La factura debe incluir al menos un producto");

        var orden = await _context.OrdenCompra.AsNoTracking().Include(o => o.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(o => o.Id == ordenId);
        if (orden == null) return (ServiceStatus.NotFound, null, "Orden no encontrada");
        if (orden.EstadoOrden is not (EstadoOrdenCompra.RecibidaParcial or EstadoOrdenCompra.Recibida))
            return (ServiceStatus.FailedValidation, null, "Solo se factura una orden con mercadería recibida y pendiente de facturar");

        var diferencias = new List<string>();
        foreach (var l in payload.Detalle)
        {
            // Bien: se matchea por ProductoId (unico en la orden). Servicio: no hay ProductoId,
            // hace falta que el cliente indique a que linea de la orden corresponde.
            OrdenCompraDetalle? od;
            if (orden.TipoOrden == TipoOrdenCompra.Servicio)
            {
                if (l.OrdenCompraDetalleId == null)
                    return (ServiceStatus.FailedValidation, null, "Indica la línea de servicio que se está facturando");
                od = orden.Detalles.FirstOrDefault(d => d.Id == l.OrdenCompraDetalleId);
            }
            else
            {
                od = l.OrdenCompraDetalleId.HasValue
                    ? orden.Detalles.FirstOrDefault(d => d.Id == l.OrdenCompraDetalleId)
                    : orden.Detalles.FirstOrDefault(d => d.ProductoId == l.ProductoId);
            }
            if (od == null)
                return (ServiceStatus.FailedValidation, null, $"El producto {l.ProductoId} no pertenece a la orden");
            if (l.Cantidad <= 0)
                return (ServiceStatus.FailedValidation, null, "Las cantidades deben ser mayores a cero");

            // El payload viaja hacia CrearCompraDeOrden: se fija aqui para que AjustarFacturadoOrden
            // (que corre despues, ya sin acceso a esta orden) matchee por Id y no por ProductoId.
            l.OrdenCompraDetalleId = od.Id;
            l.ProductoId ??= od.ProductoId;
            l.Descripcion ??= od.Descripcion;

            var nombre = od.Producto?.Nombre ?? od.Descripcion ?? $"#{od.Id}";
            var pendiente = od.CantidadRecibida - od.CantidadFacturada;
            if (l.Cantidad > pendiente)
                diferencias.Add($"{nombre}: facturas {l.Cantidad} pero solo hay {pendiente} recibidos por facturar");
            if (l.CostoUnitario != od.CostoUnitario)
                diferencias.Add($"{nombre}: precio facturado {l.CostoUnitario:0.00} vs orden {od.CostoUnitario:0.00}");
        }

        if (diferencias.Count > 0)
        {
            var config = await LeerConfiguracion();
            if ((config?.CruceFactura ?? CruceFacturaModo.Advertir) == CruceFacturaModo.Bloquear)
                return (ServiceStatus.FailedValidation, null, "La factura no cuadra con la orden/recepción: " + string.Join("; ", diferencias));
            if (!payload.ConfirmarDiferencias)
                return (ServiceStatus.FailedValidation, null, "[DIFERENCIAS] " + string.Join("; ", diferencias));
        }

        payload.OrdenCompraId = orden.Id;
        payload.ProveedorId ??= orden.ProveedorId;
        payload.SucursalId ??= orden.SucursalId;
        payload.MonedaId ??= orden.MonedaId;

        return await _compraRepository.CrearCompraDeOrden(payload);
    }
}
