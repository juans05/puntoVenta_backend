using Application.Interfaces.IRepository;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Helper de partida doble. Generar()/Reversar() son llamados desde OrdenCompraRepository
// (movimiento de inventario), CompraRepository (factura) y CuentasRepository (pago) -- ver
// PCGE.xlsx / plan de Pieza 3 para el detalle de que cuentas usa cada punto de generacion.
// Nunca borra un asiento: Reversar marca el original ANULADO y agrega uno nuevo con las lineas
// invertidas, para que el libro diario conserve el historial completo.
public class AsientoContableRepository : IAsientoContableRepository
{
    private readonly SpaContext _context;

    public AsientoContableRepository(SpaContext context)
    {
        _context = context;
    }

    private async Task<string> GenerarNumero()
        => $"AS-{((await _context.AsientoContable.IgnoreQueryFilters().CountAsync(a => a.TenantId == _context.CurrentTenantName)) + 1).ToString().PadLeft(6, '0')}";

    private static AsientoContableDto ToDto(AsientoContable a) => new()
    {
        Id = a.Id,
        Numero = a.Numero,
        Fecha = a.Fecha,
        Glosa = a.Glosa,
        OrigenTipo = a.OrigenTipo,
        OrigenId = a.OrigenId,
        EstadoAsiento = a.EstadoAsiento,
        Detalle = a.Detalle.Select(d => new AsientoContableDetalleDto
        {
            CuentaCodigo = d.CuentaContable!.Codigo,
            CuentaNombre = d.CuentaContable.Nombre,
            Debe = d.Debe,
            Haber = d.Haber
        }).ToList()
    };

    public async Task<(ServiceStatus, AsientoContableDto?, string)> Generar(string origenTipo, int origenId, string glosa, List<LineaAsientoContable> lineas)
    {
        if (lineas == null || lineas.Count < 2)
            return (ServiceStatus.FailedValidation, null, "Un asiento necesita al menos una línea al debe y una al haber");
        if (lineas.Any(l => l.Debe < 0 || l.Haber < 0))
            return (ServiceStatus.FailedValidation, null, "Los montos de un asiento no pueden ser negativos");

        var totalDebe = Math.Round(lineas.Sum(l => l.Debe), 2);
        var totalHaber = Math.Round(lineas.Sum(l => l.Haber), 2);
        if (totalDebe != totalHaber)
            return (ServiceStatus.FailedValidation, null, $"El asiento no cuadra: debe {totalDebe} vs haber {totalHaber}");

        var codigos = lineas.Select(l => l.CuentaCodigo).Distinct().ToList();
        var cuentas = await _context.CuentaContable.AsNoTracking()
            .Where(c => codigos.Contains(c.Codigo)).ToDictionaryAsync(c => c.Codigo);

        var faltante = codigos.FirstOrDefault(c => !cuentas.ContainsKey(c));
        if (faltante != null)
            return (ServiceStatus.FailedValidation, null, $"No existe la cuenta contable '{faltante}'");

        var asiento = new AsientoContable
        {
            Numero = await GenerarNumero(),
            Fecha = DateTime.UtcNow.AddHours(-5),
            Glosa = glosa,
            OrigenTipo = origenTipo,
            OrigenId = origenId,
            EstadoAsiento = EstadoAsientoContable.Emitido,
            Detalle = lineas.Select(l => new AsientoContableDetalle
            {
                CuentaContableId = cuentas[l.CuentaCodigo].Id,
                Debe = l.Debe,
                Haber = l.Haber
            }).ToList()
        };
        _context.AsientoContable.Add(asiento);
        await _context.SaveChangesAsync();

        return await ObtenerConDetalle(asiento.Id);
    }

    public async Task<(ServiceStatus, AsientoContableDto?, string)> Reversar(string origenTipo, int origenId)
    {
        var original = await _context.AsientoContable.AsTracking()
            .Include(a => a.Detalle)
            .FirstOrDefaultAsync(a => a.OrigenTipo == origenTipo && a.OrigenId == origenId && a.EstadoAsiento == EstadoAsientoContable.Emitido);
        if (original == null)
            return (ServiceStatus.NotFound, null, "No hay un asiento activo para este origen");

        original.EstadoAsiento = EstadoAsientoContable.Anulado;

        var reverso = new AsientoContable
        {
            Numero = await GenerarNumero(),
            Fecha = DateTime.UtcNow.AddHours(-5),
            Glosa = $"Reverso: {original.Glosa}",
            OrigenTipo = origenTipo,
            OrigenId = origenId,
            EstadoAsiento = EstadoAsientoContable.Emitido,
            Detalle = original.Detalle.Select(d => new AsientoContableDetalle
            {
                CuentaContableId = d.CuentaContableId,
                Debe = d.Haber,
                Haber = d.Debe
            }).ToList()
        };
        _context.AsientoContable.Add(reverso);
        await _context.SaveChangesAsync();

        return await ObtenerConDetalle(reverso.Id);
    }

    public async Task<(ServiceStatus, AsientoContableDto?, string)> GenerarBasadoEn(
        string origenTipoBase, int origenIdBase, bool invertido, string nuevoOrigenTipo, int nuevoOrigenId, string glosa)
    {
        var basado = await _context.AsientoContable.AsNoTracking()
            .Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .FirstOrDefaultAsync(a => a.OrigenTipo == origenTipoBase && a.OrigenId == origenIdBase && a.EstadoAsiento == EstadoAsientoContable.Emitido);
        if (basado == null)
            return (ServiceStatus.NotFound, null, "No hay un asiento activo para el documento original");

        var lineas = basado.Detalle.Select(d => invertido
            ? new LineaAsientoContable(d.CuentaContable!.Codigo, d.Haber, d.Debe)
            : new LineaAsientoContable(d.CuentaContable!.Codigo, d.Debe, d.Haber)).ToList();

        return await Generar(nuevoOrigenTipo, nuevoOrigenId, glosa, lineas);
    }

    public async Task<(ServiceStatus, List<AsientoContableDto>?, string)> Listar(DateTime? desde, DateTime? hasta, string? origenTipo)
    {
        var query = _context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable).AsQueryable();
        if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(a => a.Fecha < hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(origenTipo)) query = query.Where(a => a.OrigenTipo == origenTipo);

        var asientos = await query.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).ToListAsync();
        return (ServiceStatus.Ok, asientos.Select(ToDto).ToList(), "Success");
    }

    private async Task<(ServiceStatus, AsientoContableDto?, string)> ObtenerConDetalle(int id)
    {
        var asiento = await _context.AsientoContable.AsNoTracking()
            .Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .FirstOrDefaultAsync(a => a.Id == id);
        return asiento == null
            ? (ServiceStatus.InternalError, null, "No se pudo leer el asiento generado")
            : (ServiceStatus.Ok, ToDto(asiento), "Success");
    }
}
