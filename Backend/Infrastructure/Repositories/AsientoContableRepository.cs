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
        FechaDocumento = a.FechaDocumento,
        FechaVencimiento = a.FechaVencimiento,
        Glosa = a.Glosa,
        OrigenTipo = a.OrigenTipo,
        OrigenId = a.OrigenId,
        EstadoAsiento = a.EstadoAsiento,
        Detalle = a.Detalle.Select(d => new AsientoContableDetalleDto
        {
            CuentaCodigo = d.CuentaContable!.Codigo,
            CuentaNombre = d.CuentaContable.Nombre,
            Debe = d.Debe,
            Haber = d.Haber,
            CuentaAsociada = d.CuentaAsociada,
            CentroCosto1 = d.CentroCosto1?.Nombre,
            CentroCosto2 = d.CentroCosto2?.Nombre,
            CuentaDestino = d.CuentaDestino?.Codigo,
            Descripcion = d.Descripcion
        }).ToList()
    };

    // Includes que ToDto necesita para mostrar la linea completa.
    private IQueryable<AsientoContable> ConDetalle() => _context.AsientoContable.AsNoTracking()
        .Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
        .Include(a => a.Detalle).ThenInclude(d => d.CentroCosto1)
        .Include(a => a.Detalle).ThenInclude(d => d.CentroCosto2)
        .Include(a => a.Detalle).ThenInclude(d => d.CuentaDestino);

    // Copia los datos analiticos de una linea (sin cuenta ni montos).
    private static AsientoContableDetalle ConAnalitica(AsientoContableDetalle d, string? cuentaAsociada, int? cc1, int? cc2, int? destino, string? descripcion)
    {
        d.CuentaAsociada = cuentaAsociada;
        d.CentroCosto1Id = cc1;
        d.CentroCosto2Id = cc2;
        d.CuentaDestinoId = destino;
        d.Descripcion = descripcion;
        return d;
    }

    // Asiento de destino (PCGE): una linea en una cuenta de gasto de clase 62-68 con Destino activo
    // genera, por cada destino configurado, Cargo (9X) / Abono (79) por su porcentaje del monto.
    // Si la linea va al Haber (reverso del gasto), los pares se invierten. Cada par cuadra solo, asi
    // que el asiento sigue cuadrando; el ultimo destino absorbe el redondeo.
    // Una linea con CuentaDestinoId (elegida a mano) usa esa 9X al 100% contra el abono 1 configurado o 79.
    private static List<AsientoContableDetalle> LineasDestino(List<LineaAsientoContable> lineas, Dictionary<string, CuentaContable> cuentas, int? cuenta79Id)
    {
        var destino = new List<AsientoContableDetalle>();
        foreach (var l in lineas)
        {
            var c = cuentas[l.CuentaCodigo];
            if (!EsGastoConDestino(c.Codigo)) continue;
            var monto = l.Debe - l.Haber;
            if (monto == 0) continue;

            var repartos = l.CuentaDestinoId.HasValue
                ? new List<(int? Cargo, int? Abono, decimal? Pct)> { (l.CuentaDestinoId, c.CuentaAbono1Id ?? cuenta79Id, 100m) }
                : !c.Destino ? new List<(int? Cargo, int? Abono, decimal? Pct)>() : new List<(int? Cargo, int? Abono, decimal? Pct)>
                {
                    (c.CuentaCargo1Id, c.CuentaAbono1Id, c.PorcentajeDestino1),
                    (c.CuentaCargo2Id, c.CuentaAbono2Id, c.PorcentajeDestino2),
                    (c.CuentaCargo3Id, c.CuentaAbono3Id, c.PorcentajeDestino3),
                };
            repartos = repartos.Where(r => r.Cargo.HasValue && r.Abono.HasValue && r.Pct > 0).ToList();

            var absoluto = Math.Abs(monto);
            var asignado = 0m;
            for (var i = 0; i < repartos.Count; i++)
            {
                var parte = i == repartos.Count - 1 ? absoluto - asignado : Math.Round(absoluto * repartos[i].Pct!.Value / 100m, 2);
                asignado += parte;
                destino.Add(ConAnalitica(new AsientoContableDetalle { CuentaContableId = repartos[i].Cargo!.Value, Debe = monto > 0 ? parte : 0, Haber = monto > 0 ? 0 : parte },
                    l.CuentaAsociada, l.CentroCosto1Id, l.CentroCosto2Id, null, l.Descripcion));
                destino.Add(ConAnalitica(new AsientoContableDetalle { CuentaContableId = repartos[i].Abono!.Value, Debe = monto > 0 ? 0 : parte, Haber = monto > 0 ? parte : 0 },
                    l.CuentaAsociada, l.CentroCosto1Id, l.CentroCosto2Id, null, l.Descripcion));
            }
        }
        return destino;
    }

    // "Cuenta de destino" aplica solo a gastos de clase 62 a 68.
    public static bool EsGastoConDestino(string codigo)
        => codigo.Length >= 2 && int.TryParse(codigo[..2], out var clase) && clase is >= 62 and <= 68;

    public async Task<(ServiceStatus, AsientoContableDto?, string)> Generar(string origenTipo, int origenId, string glosa, List<LineaAsientoContable> lineas,
        bool aplicarDestino = true, DateTime? fechaDocumento = null, DateTime? fechaVencimiento = null, DateTime? fecha = null)
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

        // IDs que llegan en la linea (asiento manual): deben ser del tenant actual (los DbSet tienen
        // query filter por tenant, asi que "no existe" cubre tambien el id de otra empresa).
        var ccIds = lineas.SelectMany(l => new[] { l.CentroCosto1Id, l.CentroCosto2Id }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ccIds.Count > 0 && await _context.CentroCosto.CountAsync(cc => ccIds.Contains(cc.Id)) != ccIds.Count)
            return (ServiceStatus.FailedValidation, null, "Hay un centro de costo que no existe");
        var destinoIds = lineas.Where(l => l.CuentaDestinoId.HasValue).Select(l => l.CuentaDestinoId!.Value).Distinct().ToList();
        if (destinoIds.Count > 0)
        {
            if (lineas.Any(l => l.CuentaDestinoId.HasValue && !EsGastoConDestino(l.CuentaCodigo)))
                return (ServiceStatus.FailedValidation, null, "La cuenta de destino solo aplica a gastos de las clases 62 a 68");
            if (await _context.CuentaContable.CountAsync(c => destinoIds.Contains(c.Id) && c.Codigo.StartsWith("9")) != destinoIds.Count)
                return (ServiceStatus.FailedValidation, null, "La cuenta de destino debe ser una cuenta existente de la clase 9");
        }

        var cuenta79Id = lineas.Any(l => l.CuentaDestinoId.HasValue)
            ? await _context.CuentaContable.AsNoTracking().Where(c => c.Codigo == "79").Select(c => (int?)c.Id).FirstOrDefaultAsync()
            : null;

        var asiento = new AsientoContable
        {
            Numero = await GenerarNumero(),
            Fecha = fecha ?? DateTime.UtcNow.AddHours(-5),
            FechaDocumento = fechaDocumento,
            FechaVencimiento = fechaVencimiento,
            Glosa = glosa,
            OrigenTipo = origenTipo,
            OrigenId = origenId,
            EstadoAsiento = EstadoAsientoContable.Emitido,
            Detalle = lineas.Select(l => ConAnalitica(new AsientoContableDetalle
            {
                CuentaContableId = cuentas[l.CuentaCodigo].Id,
                Debe = l.Debe,
                Haber = l.Haber
            }, l.CuentaAsociada, l.CentroCosto1Id, l.CentroCosto2Id, l.CuentaDestinoId, l.Descripcion))
            .Concat(aplicarDestino ? LineasDestino(lineas, cuentas, cuenta79Id) : new()).ToList()
        };
        _context.AsientoContable.Add(asiento);
        await _context.SaveChangesAsync();

        return await ObtenerConDetalle(asiento.Id);
    }

    // Asiento manual: solo cuentas de ultimo nivel (8 digitos) y CC1 cuando la cuenta lo exige.
    // Generar valida que cuadre, que las cuentas/centros existan en el tenant y el destino 9X.
    public async Task<(ServiceStatus, AsientoContableDto?, string)> CrearManual(CrearAsientoManualPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Glosa))
            return (ServiceStatus.FailedValidation, null, "Escribe una descripción para el asiento");
        var lineas = payload.Lineas?.Where(l => l.Debe != 0 || l.Haber != 0).ToList() ?? new();
        if (lineas.Any(l => l.Debe > 0 && l.Haber > 0))
            return (ServiceStatus.FailedValidation, null, "Una línea no puede tener débito y crédito a la vez");
        if (lineas.Any(l => string.IsNullOrWhiteSpace(l.CuentaCodigo) || l.CuentaCodigo.Length != 8))
            return (ServiceStatus.FailedValidation, null, "Cada línea debe usar una cuenta de último nivel (8 dígitos)");

        var codigos = lineas.Select(l => l.CuentaCodigo).Distinct().ToList();
        var exigenCentro = await _context.CuentaContable.AsNoTracking()
            .Where(c => codigos.Contains(c.Codigo) && c.ModoCentroCosto == ModoCentroCostoCuenta.Obligatorio).Select(c => c.Codigo).ToListAsync();
        if (lineas.FirstOrDefault(l => exigenCentro.Contains(l.CuentaCodigo) && l.CentroCosto1Id == null) is { } sinCentro)
            return (ServiceStatus.FailedValidation, null, $"La cuenta {sinCentro.CuentaCodigo} exige centro de costos");

        await _context.Database.BeginTransactionAsync();
        try
        {
            var (estado, dto, mensaje) = await Generar(OrigenAsientoContable.Manual, 0, payload.Glosa.Trim(), lineas,
                fechaDocumento: payload.FechaDocumento, fechaVencimiento: payload.FechaVencimiento, fecha: payload.Fecha == default ? null : payload.Fecha);
            if (estado != ServiceStatus.Ok)
            {
                await _context.Database.RollbackTransactionAsync();
                return (estado, null, mensaje);
            }

            // OrigenId = su propio Id, para poder ubicarlo/reversarlo como cualquier otro origen.
            var asiento = await _context.AsientoContable.AsTracking().FirstAsync(a => a.Id == dto!.Id);
            asiento.OrigenId = asiento.Id;
            await _context.SaveChangesAsync();
            await _context.Database.CommitTransactionAsync();
            return await ObtenerConDetalle(asiento.Id);
        }
        catch (Exception e)
        {
            await _context.Database.RollbackTransactionAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al registrar el asiento -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, AsientoContableDto?, string)> AnularManual(int id)
    {
        var estados = await _context.AsientoContable.AsNoTracking()
            .Where(a => a.OrigenTipo == OrigenAsientoContable.Manual && a.OrigenId == id).Select(a => a.EstadoAsiento).ToListAsync();
        if (estados.Count == 0) return (ServiceStatus.NotFound, null, "Asiento manual no encontrado");
        if (estados.Contains(EstadoAsientoContable.Anulado)) return (ServiceStatus.FailedValidation, null, "El asiento ya está anulado");
        return await Reversar(OrigenAsientoContable.Manual, id);
    }

    public async Task<string> CodigoCuenta(int? cuentaId, string porDefecto)
        => cuentaId.HasValue
            ? await _context.CuentaContable.AsNoTracking().Where(c => c.Id == cuentaId).Select(c => c.Codigo).FirstOrDefaultAsync() ?? porDefecto
            : porDefecto;

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
            FechaDocumento = original.FechaDocumento,
            FechaVencimiento = original.FechaVencimiento,
            Glosa = $"Reverso: {original.Glosa}",
            OrigenTipo = origenTipo,
            OrigenId = origenId,
            EstadoAsiento = EstadoAsientoContable.Emitido,
            Detalle = original.Detalle.Select(d => ConAnalitica(new AsientoContableDetalle
            {
                CuentaContableId = d.CuentaContableId,
                Debe = d.Haber,
                Haber = d.Debe
            }, d.CuentaAsociada, d.CentroCosto1Id, d.CentroCosto2Id, d.CuentaDestinoId, d.Descripcion)).ToList()
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

        var lineas = basado.Detalle.Select(d => new LineaAsientoContable(d.CuentaContable!.Codigo, invertido ? d.Haber : d.Debe, invertido ? d.Debe : d.Haber)
        {
            CuentaAsociada = d.CuentaAsociada, CentroCosto1Id = d.CentroCosto1Id, CentroCosto2Id = d.CentroCosto2Id,
            CuentaDestinoId = d.CuentaDestinoId, Descripcion = d.Descripcion
        }).ToList();

        return await Generar(nuevoOrigenTipo, nuevoOrigenId, glosa, lineas, aplicarDestino: false); // el original ya trae sus lineas de destino
    }

    public async Task<(ServiceStatus, List<AsientoContableDto>?, string)> Listar(DateTime? desde, DateTime? hasta, string? origenTipo)
    {
        var query = ConDetalle();
        if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(a => a.Fecha < hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(origenTipo)) query = query.Where(a => a.OrigenTipo == origenTipo);

        var asientos = await query.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).ToListAsync();
        return (ServiceStatus.Ok, asientos.Select(ToDto).ToList(), "Success");
    }

    public async Task<(ServiceStatus, EstadoResultadosDto?, string)> ObtenerEstadoResultados(DateTime? desde, DateTime? hasta)
    {
        var query = _context.AsientoContableDetalle.AsNoTracking().Include(d => d.CuentaContable)
            .Where(d => d.AsientoContable!.EstadoAsiento == EstadoAsientoContable.Emitido
                     && d.AsientoContable.OrigenTipo != "CierreAnual" // el cierre anula los resultados; el reporte los muestra antes del cierre
                     && (d.CuentaContable!.Tipo == TipoCuentaContable.Ingreso || d.CuentaContable.Tipo == TipoCuentaContable.Gasto));
        if (desde.HasValue) query = query.Where(d => d.AsientoContable!.Fecha >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(d => d.AsientoContable!.Fecha < hasta.Value.Date.AddDays(1));

        // Clase 9 (destino/analitica) y 79 (cargas imputables) se compensan entre si: el estado de
        // resultados por naturaleza (clases 6/7) no las muestra, si no inflarian ingresos y gastos.
        var filas = (await query.ToListAsync())
            .Where(d => !d.CuentaContable!.Codigo.StartsWith("9") && !d.CuentaContable.Codigo.StartsWith("79")).ToList();

        var ingresos = AgruparPorCuenta(filas, TipoCuentaContable.Ingreso, naturalezaDeudora: false);
        var gastos = AgruparPorCuenta(filas, TipoCuentaContable.Gasto, naturalezaDeudora: true);
        var totalIngresos = ingresos.Sum(l => l.Monto);
        var totalGastos = gastos.Sum(l => l.Monto);

        return (ServiceStatus.Ok, new EstadoResultadosDto
        {
            Ingresos = ingresos,
            Gastos = gastos,
            TotalIngresos = totalIngresos,
            TotalGastos = totalGastos,
            UtilidadNeta = totalIngresos - totalGastos
        }, "Success");
    }

    // Sin asientos de cierre de periodo: el Balance General "a la fecha" suma todo el historico
    // hasta esa fecha (naturaleza deudora para Activo, acreedora para Pasivo/Patrimonio), y agrega
    // la Utilidad del Ejercicio (Ingresos - Gastos acumulados) como linea sintetica de Patrimonio
    // para que Activo == Pasivo + Patrimonio siempre cuadre (identidad contable basica).
    public async Task<(ServiceStatus, BalanceGeneralDto?, string)> ObtenerBalanceGeneral(DateTime hasta)
    {
        var filas = await _context.AsientoContableDetalle.AsNoTracking().Include(d => d.CuentaContable)
            .Where(d => d.AsientoContable!.EstadoAsiento == EstadoAsientoContable.Emitido && d.AsientoContable.Fecha < hasta.Date.AddDays(1))
            .ToListAsync();

        var activo = AgruparPorCuenta(filas, TipoCuentaContable.Activo, naturalezaDeudora: true);
        var pasivo = AgruparPorCuenta(filas, TipoCuentaContable.Pasivo, naturalezaDeudora: false);
        var patrimonio = AgruparPorCuenta(filas, TipoCuentaContable.Patrimonio, naturalezaDeudora: false);

        var totalIngresos = filas.Where(d => d.CuentaContable!.Tipo == TipoCuentaContable.Ingreso).Sum(d => d.Haber - d.Debe);
        var totalGastos = filas.Where(d => d.CuentaContable!.Tipo == TipoCuentaContable.Gasto).Sum(d => d.Debe - d.Haber);
        var resultado = totalIngresos - totalGastos;

        return (ServiceStatus.Ok, new BalanceGeneralDto
        {
            Activo = activo,
            Pasivo = pasivo,
            Patrimonio = patrimonio,
            ResultadoDelEjercicio = resultado,
            TotalActivo = activo.Sum(l => l.Monto),
            TotalPasivoYPatrimonio = pasivo.Sum(l => l.Monto) + patrimonio.Sum(l => l.Monto) + resultado
        }, "Success");
    }

    private static List<LineaReporteContableDto> AgruparPorCuenta(List<AsientoContableDetalle> filas, string tipo, bool naturalezaDeudora) =>
        filas.Where(d => d.CuentaContable!.Tipo == tipo)
             .GroupBy(d => d.CuentaContable!)
             .Select(g => new LineaReporteContableDto
             {
                 CuentaCodigo = g.Key.Codigo,
                 CuentaNombre = g.Key.Nombre,
                 Tipo = g.Key.Tipo,
                 Monto = naturalezaDeudora ? g.Sum(d => d.Debe - d.Haber) : g.Sum(d => d.Haber - d.Debe)
             })
             .Where(l => l.Monto != 0)
             .OrderBy(l => l.CuentaCodigo)
             .ToList();

    private async Task<(ServiceStatus, AsientoContableDto?, string)> ObtenerConDetalle(int id)
    {
        var asiento = await ConDetalle().FirstOrDefaultAsync(a => a.Id == id);
        return asiento == null
            ? (ServiceStatus.InternalError, null, "No se pudo leer el asiento generado")
            : (ServiceStatus.Ok, ToDto(asiento), "Success");
    }
}
