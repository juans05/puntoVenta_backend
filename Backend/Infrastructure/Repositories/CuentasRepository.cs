using Application.Helper;
using Application.Interfaces.IRepository;
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

// Cuentas por cobrar / por pagar (estilo SAP B1: Pagos recibidos / Pagos efectuados).
// El saldo se calcula siempre (total - pagos aplicados); nunca se guarda.
//  - Venta a credito: total - suma(Pago). Un cobro crea un Pago por documento; anularlo, un Pago negativo.
//  - Compra a credito: total - suma(PagoProveedorDetalle de pagos ACTIVOS).
public class CuentasRepository : ICuentasRepository
{
    private readonly SpaContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public CuentasRepository(SpaContext context, IHttpContextAccessor? httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? PaisIdClaim =>
        _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimConstants.Pais) is { } claim
            && int.TryParse(claim, out var pais) ? pais : (int?)null;

    private DateTime NowLocal() => DateTimeHelper.LocalNow(PaisIdClaim);

    private sealed record Doc(int Id, int SocioId, string Socio, string? SocioDoc, string Numero,
        DateTime Fecha, DateTime Vencimiento, decimal Total, decimal Saldo);

    private const decimal Tolerancia = 0.005m;

    // Documentos a credito con saldo > 0 (opcionalmente de un solo socio o de ciertos ids).
    private async Task<List<Doc>> CargarDocumentos(bool cobrar, int? socioId = null, ICollection<int>? ids = null)
    {
        if (cobrar)
        {
            var ventas = await _context.ComprobanteCabecera.AsNoTracking()
                .Where(c => c.EsCredito && c.EstadoComprobante != EstatusComprobante.Anulado && c.ClienteId != null
                         && (c.TipoDocumentoVentaId == (int)TipoComprobante.Factura
                          || c.TipoDocumentoVentaId == (int)TipoComprobante.Boleta
                          || c.TipoDocumentoVentaId == (int)TipoComprobante.TicketInterno))
                .Where(c => socioId == null || c.ClienteId == socioId)
                .Where(c => ids == null || ids.Contains(c.Id))
                .Select(c => new
                {
                    c.Id, ClienteId = c.ClienteId!.Value, c.Serie, c.Correlativo, c.FechaVenta, c.FechaCreacion,
                    c.FechaVencimiento, c.ValorTotal
                }).ToListAsync();

            // Pagos sumados en memoria (SQLite no agrega decimales en SQL; asi funciona igual en Postgres).
            var ventaIds = ventas.Select(v => v.Id).ToList();
            var pagosVenta = (await _context.Pago.AsNoTracking().Where(p => ventaIds.Contains(p.ComprobanteCabeceraId))
                .Select(p => new { p.ComprobanteCabeceraId, p.Monto }).ToListAsync())
                .GroupBy(p => p.ComprobanteCabeceraId).ToDictionary(g => g.Key, g => g.Sum(p => p.Monto));

            var clienteIds = ventas.Select(v => v.ClienteId).Distinct().ToList();
            var clientes = await _context.Cliente.AsNoTracking().Where(c => clienteIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            return ventas.Select(v =>
            {
                var fecha = v.FechaVenta ?? v.FechaCreacion;
                clientes.TryGetValue(v.ClienteId, out var cli);
                return new Doc(v.Id, v.ClienteId, cli?.Nombre ?? "Sin nombre", cli?.NumeroDocumento,
                    $"{v.Serie}-{v.Correlativo.ToString().PadLeft(7, '0')}", fecha, v.FechaVencimiento ?? fecha,
                    v.ValorTotal, v.ValorTotal - pagosVenta.GetValueOrDefault(v.Id));
            }).Where(d => d.Saldo > Tolerancia).ToList();
        }

        var compras = await _context.Compra.AsNoTracking()
            .Where(c => c.EsCredito && c.Estado != "ANULADO" && c.ProveedorId != null)
            .Where(c => socioId == null || c.ProveedorId == socioId)
            .Where(c => ids == null || ids.Contains(c.Id))
            .Select(c => new
            {
                c.Id, ProveedorId = c.ProveedorId!.Value, c.NumeroCompra, c.Serie, c.Numero, c.FechaCompra,
                c.FechaVencimiento, c.Total
            }).ToListAsync();

        var compraIds = compras.Select(c => c.Id).ToList();
        var pagosCompra = (await _context.PagoProveedorDetalle.AsNoTracking()
            .Where(d => compraIds.Contains(d.CompraId) && d.PagoProveedor!.EstadoPago == EstadoPagoCuenta.Activo)
            .Select(d => new { d.CompraId, d.MontoAplicado }).ToListAsync())
            .GroupBy(d => d.CompraId).ToDictionary(g => g.Key, g => g.Sum(d => d.MontoAplicado));

        var proveedorIds = compras.Select(c => c.ProveedorId).Distinct().ToList();
        var proveedores = await _context.Proveedor.AsNoTracking().Where(p => proveedorIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        return compras.Select(c =>
        {
            proveedores.TryGetValue(c.ProveedorId, out var prov);
            var numero = !string.IsNullOrWhiteSpace(c.Serie) && !string.IsNullOrWhiteSpace(c.Numero)
                ? $"{c.Serie}-{c.Numero}" : c.NumeroCompra;
            return new Doc(c.Id, c.ProveedorId, prov?.Nombre ?? "Sin nombre", prov?.Ruc, numero, c.FechaCompra,
                c.FechaVencimiento ?? c.FechaCompra, c.Total, c.Total - pagosCompra.GetValueOrDefault(c.Id));
        }).Where(d => d.Saldo > Tolerancia).ToList();
    }

    private static int DiasVencido(Doc d, DateTime hoy) => (int)(hoy.Date - d.Vencimiento.Date).TotalDays;

    public async Task<(ServiceStatus, List<SocioSaldoDto>?, string)> Resumen(bool cobrar)
    {
        var hoy = NowLocal();
        var docs = await CargarDocumentos(cobrar);
        var lista = docs.GroupBy(d => d.SocioId).Select(g => new SocioSaldoDto
        {
            Id = g.Key,
            Nombre = g.First().Socio,
            Documento = g.First().SocioDoc,
            Saldo = g.Sum(d => d.Saldo),
            Vencido = g.Where(d => DiasVencido(d, hoy) > 0).Sum(d => d.Saldo),
            Documentos = g.Count()
        }).OrderByDescending(s => s.Saldo).ToList();
        return (ServiceStatus.Ok, lista, "Success");
    }

    public async Task<(ServiceStatus, List<DocumentoSaldoDto>?, string)> Documentos(bool cobrar, int socioId)
    {
        var hoy = NowLocal();
        var docs = await CargarDocumentos(cobrar, socioId);
        return (ServiceStatus.Ok, docs.OrderBy(d => d.Vencimiento).Select(d => new DocumentoSaldoDto
        {
            Id = d.Id,
            Numero = d.Numero,
            Fecha = d.Fecha.ToString("dd/MM/yyyy"),
            Vencimiento = d.Vencimiento.ToString("dd/MM/yyyy"),
            Total = d.Total,
            Saldo = d.Saldo,
            DiasVencido = Math.Max(0, DiasVencido(d, hoy))
        }).ToList(), "Success");
    }

    public async Task<(ServiceStatus, List<AntiguedadDto>?, string)> Antiguedad(bool cobrar)
    {
        var hoy = NowLocal();
        var docs = await CargarDocumentos(cobrar);
        var lista = docs.GroupBy(d => d.SocioId).Select(g =>
        {
            decimal Tramo(Func<int, bool> f) => g.Where(d => f(DiasVencido(d, hoy))).Sum(d => d.Saldo);
            return new AntiguedadDto
            {
                Id = g.Key,
                Nombre = g.First().Socio,
                PorVencer = Tramo(x => x <= 0),
                Dias1a30 = Tramo(x => x >= 1 && x <= 30),
                Dias31a60 = Tramo(x => x >= 31 && x <= 60),
                Dias61a90 = Tramo(x => x >= 61 && x <= 90),
                Mas90 = Tramo(x => x > 90),
                Total = g.Sum(d => d.Saldo)
            };
        }).OrderByDescending(a => a.Total).ToList();
        return (ServiceStatus.Ok, lista, "Success");
    }

    // ---------- Registrar / anular ----------

    private static string? ValidarPayload(RegistrarPagoCuentaPayload p)
    {
        if (p.Detalle == null || p.Detalle.Count == 0) return "Selecciona al menos un documento a pagar";
        if (p.Detalle.Any(d => d.Monto <= 0)) return "Los montos deben ser mayores a cero";
        if (p.Detalle.GroupBy(d => d.DocumentoId).Any(g => g.Count() > 1)) return "Un documento no puede repetirse";
        return null;
    }

    private async Task<Caja?> CajaAbiertaDelUsuario()
    {
        var user = _httpContextAccessor?.HttpContext?.User.FindFirstValue("username")?.ToUpper();
        if (string.IsNullOrEmpty(user)) return null;
        var hoy = NowLocal().Date;
        var sucursalId = _context.CurrentSucursalId;
        return await _context.Caja.AsTracking()
            .Where(x => x.UsuarioCreacion == user && x.FechaCreacion.Date == hoy && x.FechaHoraCierre == null && x.SucursalId == sucursalId)
            .FirstOrDefaultAsync();
    }

    public async Task<(ServiceStatus, PagoCuentaDto?, string)> RegistrarPago(bool cobrar, RegistrarPagoCuentaPayload payload)
    {
        if (ValidarPayload(payload) is { } error) return (ServiceStatus.FailedValidation, null, error);
        if (!await _context.Metodopago.AnyAsync(m => m.Id == payload.MetodoPagoId))
            return (ServiceStatus.FailedValidation, null, "Medio de pago no válido");

        var docs = await CargarDocumentos(cobrar, payload.SocioId, payload.Detalle.Select(d => d.DocumentoId).ToList());
        foreach (var l in payload.Detalle)
        {
            var doc = docs.FirstOrDefault(d => d.Id == l.DocumentoId);
            if (doc == null)
                return (ServiceStatus.FailedValidation, null, "Un documento no está pendiente de pago o no es de este " + (cobrar ? "cliente" : "proveedor"));
            if (l.Monto > doc.Saldo + Tolerancia)
                return (ServiceStatus.FailedValidation, null, $"{doc.Numero}: el monto {l.Monto:0.00} supera el saldo {doc.Saldo:0.00}");
        }

        var efectivo = payload.MetodoPagoId == (int)TipoPago.Efectivo;
        Caja? caja = null;
        if (!cobrar && efectivo)
        {
            caja = await CajaAbiertaDelUsuario();
            if (caja == null) return (ServiceStatus.FailedValidation, null, "Abre tu caja para pagar en efectivo");
        }

        var total = Math.Round(payload.Detalle.Sum(d => d.Monto), 2);

        await _context.Database.BeginTransactionAsync();
        try
        {
            int id;
            string numero;
            if (cobrar)
            {
                numero = $"COB-{((await _context.Cobranza.IgnoreQueryFilters().CountAsync(c => c.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";
                var cobranza = new Cobranza
                {
                    Numero = numero, ClienteId = payload.SocioId, Fecha = NowLocal(), MetodoPagoId = payload.MetodoPagoId,
                    Monto = total, Referencia = payload.Referencia, Observacion = payload.Observacion,
                    Detalles = payload.Detalle.Select(d => new CobranzaDetalle
                    {
                        ComprobanteCabeceraId = d.DocumentoId, MontoAplicado = Math.Round(d.Monto, 2)
                    }).ToList()
                };
                _context.Cobranza.Add(cobranza);
                // Un Pago por documento: el cierre de caja del dia recoge los Pago sin caja del usuario.
                foreach (var d in cobranza.Detalles)
                    _context.Pago.Add(new Pago { ComprobanteCabeceraId = d.ComprobanteCabeceraId, MetodoPagoId = payload.MetodoPagoId, Monto = d.MontoAplicado });
                await _context.SaveChangesAsync();
                id = cobranza.Id;
            }
            else
            {
                numero = $"PAG-{((await _context.PagoProveedor.IgnoreQueryFilters().CountAsync(c => c.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";
                var pago = new PagoProveedor
                {
                    Numero = numero, ProveedorId = payload.SocioId, Fecha = NowLocal(), MetodoPagoId = payload.MetodoPagoId,
                    Monto = total, Referencia = payload.Referencia, Observacion = payload.Observacion,
                    Detalles = payload.Detalle.Select(d => new PagoProveedorDetalle
                    {
                        CompraId = d.DocumentoId, MontoAplicado = Math.Round(d.Monto, 2)
                    }).ToList()
                };
                _context.PagoProveedor.Add(pago);
                if (caja != null)
                    _context.Retiros.Add(new Retiros { CajaId = caja.Id, Monto = total, Motivo = $"Pago proveedor {numero}" });
                await _context.SaveChangesAsync();
                id = pago.Id;
            }

            await _context.Database.CommitTransactionAsync();
            return await ObtenerPago(cobrar, id);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al registrar {(cobrar ? "cobro" : "pago")} -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, PagoCuentaDto?, string)> AnularPago(bool cobrar, int id)
    {
        await _context.Database.BeginTransactionAsync();
        try
        {
            if (cobrar)
            {
                var cobranza = await _context.Cobranza.AsTracking().Include(c => c.Detalles).FirstOrDefaultAsync(c => c.Id == id);
                if (cobranza == null) { await _context.Database.RollbackTransactionAsync(); return (ServiceStatus.NotFound, null, "Cobro no encontrado"); }
                if (cobranza.EstadoCobranza != EstadoPagoCuenta.Activo)
                { await _context.Database.RollbackTransactionAsync(); return (ServiceStatus.FailedValidation, null, "El cobro ya está anulado"); }

                // Movimiento inverso (Pago negativo): el saldo vuelve y el cierre de caja de hoy lo refleja.
                foreach (var d in cobranza.Detalles)
                    _context.Pago.Add(new Pago { ComprobanteCabeceraId = d.ComprobanteCabeceraId, MetodoPagoId = cobranza.MetodoPagoId, Monto = -d.MontoAplicado });
                cobranza.EstadoCobranza = EstadoPagoCuenta.Anulado;
            }
            else
            {
                var pago = await _context.PagoProveedor.AsTracking().FirstOrDefaultAsync(c => c.Id == id);
                if (pago == null) { await _context.Database.RollbackTransactionAsync(); return (ServiceStatus.NotFound, null, "Pago no encontrado"); }
                if (pago.EstadoPago != EstadoPagoCuenta.Activo)
                { await _context.Database.RollbackTransactionAsync(); return (ServiceStatus.FailedValidation, null, "El pago ya está anulado"); }

                if (pago.MetodoPagoId == (int)TipoPago.Efectivo)
                {
                    var caja = await CajaAbiertaDelUsuario();
                    if (caja == null) { await _context.Database.RollbackTransactionAsync(); return (ServiceStatus.FailedValidation, null, "Abre tu caja para anular un pago en efectivo"); }
                    _context.Retiros.Add(new Retiros { CajaId = caja.Id, Monto = -pago.Monto, Motivo = $"Anulación pago proveedor {pago.Numero}" });
                }
                pago.EstadoPago = EstadoPagoCuenta.Anulado;
            }

            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();
            return await ObtenerPago(cobrar, id);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al anular -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    // ---------- Consulta de pagos ----------

    private async Task<(ServiceStatus, PagoCuentaDto?, string)> ObtenerPago(bool cobrar, int id)
    {
        var (_, lista, _) = await ListarPagos(cobrar, new PagosCuentaQueryParams { Page = 1, Amount = 1, SocioId = null }, id);
        var dto = lista?.Items?.FirstOrDefault();
        return dto == null ? (ServiceStatus.NotFound, null, "No encontrado") : (ServiceStatus.Ok, dto, "Success");
    }

    public Task<(ServiceStatus, DataCollection<PagoCuentaDto>?, string)> ListarPagos(bool cobrar, PagosCuentaQueryParams payload)
        => ListarPagos(cobrar, payload, null);

    private async Task<(ServiceStatus, DataCollection<PagoCuentaDto>?, string)> ListarPagos(bool cobrar, PagosCuentaQueryParams payload, int? soloId)
    {
        try
        {
            var page = Math.Max(1, payload.Page);
            var amount = Math.Max(1, payload.Amount);
            List<PagoCuentaDto> items;
            int total;

            if (cobrar)
            {
                var q = _context.Cobranza.AsNoTracking().Include(c => c.Cliente).Include(c => c.Metodopago)
                    .Include(c => c.Detalles).ThenInclude(d => d.ComprobanteCabecera)
                    .Where(c => (payload.SocioId == null || c.ClienteId == payload.SocioId) && (soloId == null || c.Id == soloId));
                total = await q.CountAsync();
                var filas = await q.OrderByDescending(c => c.Id).Skip((page - 1) * amount).Take(amount).ToListAsync();
                items = filas.Select(c => new PagoCuentaDto
                {
                    Id = c.Id, Numero = c.Numero, SocioId = c.ClienteId, Socio = c.Cliente?.Nombre,
                    Fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"), MetodoPago = c.Metodopago?.Nombre, Monto = c.Monto,
                    Referencia = c.Referencia, Estado = c.EstadoCobranza,
                    Detalle = c.Detalles.Select(d => new PagoCuentaDetalleDto
                    {
                        DocumentoId = d.ComprobanteCabeceraId,
                        Documento = d.ComprobanteCabecera == null ? "" : $"{d.ComprobanteCabecera.Serie}-{d.ComprobanteCabecera.Correlativo.ToString().PadLeft(7, '0')}",
                        MontoAplicado = d.MontoAplicado
                    }).ToList()
                }).ToList();
            }
            else
            {
                var q = _context.PagoProveedor.AsNoTracking().Include(c => c.Proveedor).Include(c => c.Metodopago)
                    .Include(c => c.Detalles).ThenInclude(d => d.Compra)
                    .Where(c => (payload.SocioId == null || c.ProveedorId == payload.SocioId) && (soloId == null || c.Id == soloId));
                total = await q.CountAsync();
                var filas = await q.OrderByDescending(c => c.Id).Skip((page - 1) * amount).Take(amount).ToListAsync();
                items = filas.Select(c => new PagoCuentaDto
                {
                    Id = c.Id, Numero = c.Numero, SocioId = c.ProveedorId, Socio = c.Proveedor?.Nombre,
                    Fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"), MetodoPago = c.Metodopago?.Nombre, Monto = c.Monto,
                    Referencia = c.Referencia, Estado = c.EstadoPago,
                    Detalle = c.Detalles.Select(d => new PagoCuentaDetalleDto
                    {
                        DocumentoId = d.CompraId,
                        Documento = d.Compra == null ? "" : (!string.IsNullOrWhiteSpace(d.Compra.Serie) && !string.IsNullOrWhiteSpace(d.Compra.Numero)
                            ? $"{d.Compra.Serie}-{d.Compra.Numero}" : d.Compra.NumeroCompra),
                        MontoAplicado = d.MontoAplicado
                    }).ToList()
                }).ToList();
            }

            return (ServiceStatus.Ok, new DataCollection<PagoCuentaDto>
            {
                Items = items, Total = total, Page = page, Pages = (int)Math.Ceiling(total / (double)amount)
            }, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}
