using Application.Interfaces.IRepository;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Asistente de cierre de año fiscal. Todo se calcula en memoria sobre un diccionario de saldos
// (deudor = Debe - Haber) y se persiste con UN SaveChanges, asi las 4 cuentas de asientos
// (fases 2 a 5) quedan todas o ninguna.
//   Fase 2: clases 6, 7 y 9 -> clase 8 (via Cuenta Cierre de cada cuenta) + Impuesto a la Renta (88 / 4017)
//   Fase 3: clase 8 -> 89 (Determinacion del Resultado). Deja 6, 7, 8 y 9 en cero.
//   Fase 4: clases 1 a 5 y 89 -> su Cuenta Cierre (Balance en cero al 31/12)
//   Fase 5: apertura 01/01 del año siguiente: saldos de 1 a 5, resultado a 591 (utilidad) / 592 (perdida)
public class CierreAnualRepository : ICierreAnualRepository
{
    private const string OrigenCierre = "CierreAnual";
    private const string OrigenApertura = "AperturaAnual";
    // Cuentas que el asistente mueve por codigo. Se resuelven a la cuenta de ultimo nivel (8 digitos)
    // bajo ese prefijo, que es la unica que admite asientos.
    private static readonly string[] CuentasRequeridas = { "88", "89", "4017", "591", "592" };

    private readonly SpaContext _context;

    public CierreAnualRepository(SpaContext context)
    {
        _context = context;
    }

    private record Linea(int CuentaId, decimal Debe, decimal Haber);
    private record AsientoPlan(int Fase, string Origen, DateTime Fecha, string Glosa, List<Linea> Lineas);
    private record Plan(List<AsientoPlan> Asientos, decimal ResultadoAntesImpuestos, decimal Impuesto);

    private static bool EsGestion(string c) => c[0] is '6' or '7' or '9';
    private static bool EsBalance(string c) => c[0] is >= '1' and <= '5';

    public async Task<(ServiceStatus, CierreAnualValidacionDto?, string)> Validar(CierreAnualPayload payload)
    {
        var (validacion, _) = await Evaluar(payload);
        return (ServiceStatus.Ok, validacion, "Success");
    }

    public async Task<(ServiceStatus, CierreAnualResultadoDto?, string)> Ejecutar(CierreAnualPayload payload)
    {
        var (validacion, plan) = await Evaluar(payload);
        if (!validacion.PuedeCerrar || plan == null)
        {
            var falla = validacion.Checklist.First(i => !i.Ok);
            return (ServiceStatus.FailedValidation, null, $"No se puede cerrar el año: {falla.Descripcion}. {falla.Detalle}");
        }

        var baseNumero = await _context.AsientoContable.IgnoreQueryFilters().CountAsync(a => a.TenantId == _context.CurrentTenantName);
        var resultado = new CierreAnualResultadoDto
        {
            ResultadoAntesDeImpuestos = plan.ResultadoAntesImpuestos,
            ImpuestoRenta = plan.Impuesto,
            ResultadoNeto = plan.ResultadoAntesImpuestos - plan.Impuesto
        };
        foreach (var a in plan.Asientos)
        {
            var asiento = new AsientoContable
            {
                Numero = $"AS-{(++baseNumero).ToString().PadLeft(6, '0')}",
                Fecha = a.Fecha,
                Glosa = a.Glosa,
                OrigenTipo = a.Origen,
                OrigenId = payload.Anio,
                EstadoAsiento = EstadoAsientoContable.Emitido,
                Detalle = a.Lineas.Select(l => new AsientoContableDetalle { CuentaContableId = l.CuentaId, Debe = l.Debe, Haber = l.Haber }).ToList()
            };
            _context.AsientoContable.Add(asiento);
            resultado.Fases.Add(new CierreAnualFaseDto { Fase = a.Fase, Nombre = a.Glosa, AsientoNumero = asiento.Numero, Total = a.Lineas.Sum(l => l.Debe) });
        }
        await _context.SaveChangesAsync();
        return (ServiceStatus.Ok, resultado, "Cierre del año ejecutado");
    }

    // Fase 1 (checklist) + armado del plan de las fases 2 a 5 (dry-run, no persiste nada).
    private async Task<(CierreAnualValidacionDto, Plan?)> Evaluar(CierreAnualPayload payload)
    {
        var v = new CierreAnualValidacionDto();
        void Item(string desc, bool ok, string? detalle = null) => v.Checklist.Add(new() { Descripcion = desc, Ok = ok, Detalle = detalle });

        Item("Año y tasa de Impuesto a la Renta válidos", payload.Anio is >= 2000 and <= 2100 && payload.TasaImpuestoRenta is >= 0 and <= 100,
            "El año debe ser 2000-2100 y la tasa entre 0 y 100");

        var yaCerrado = await _context.AsientoContable.AnyAsync(a => a.OrigenTipo == OrigenCierre && a.OrigenId == payload.Anio && a.EstadoAsiento == EstadoAsientoContable.Emitido);
        Item("El año fiscal no está cerrado", !yaCerrado, $"El año {payload.Anio} ya tiene asientos de cierre");

        var pendientes = await _context.OrdenCompra.CountAsync(o => o.EstadoOrden == EstadoOrdenCompra.Borrador)
                       + await _context.PedidoVenta.CountAsync(p => p.EstadoPedidoVenta == EstadosPedidoVenta.Borrador);
        Item("Sin documentos pendientes de validación", pendientes == 0, $"{pendientes} documento(s) en borrador (órdenes de compra / pedidos de venta)");

        Item("Diferencia de cambio (NIC 21) ejecutada al 31 de diciembre", payload.ConfirmaDifCambio,
            "El sistema no puede verificarlo: confirma que ya reexpresaste las cuentas monetarias");

        var malDestino = await _context.CuentaContable.AsNoTracking()
            .Where(c => c.Destino && ((c.PorcentajeDestino1 ?? 0) + (c.PorcentajeDestino2 ?? 0) + (c.PorcentajeDestino3 ?? 0)) != 100)
            .Select(c => c.Codigo).Take(10).ToListAsync();
        Item("Asientos de destino suman 100%", malDestino.Count == 0, "Cuentas con porcentajes ≠ 100%: " + string.Join(", ", malDestino));

        var montos = await _context.AsientoContableDetalle.AsNoTracking()
            .Where(d => d.AsientoContable!.EstadoAsiento == EstadoAsientoContable.Emitido)
            .Select(d => new { d.Debe, d.Haber }).ToListAsync();
        var totDebe = montos.Sum(m => m.Debe);
        var totHaber = montos.Sum(m => m.Haber);
        Item("El libro diario cuadra (Debe = Haber)", Math.Round(totDebe - totHaber, 2) == 0, $"Debe {totDebe} vs Haber {totHaber}");

        var cuentas = await _context.CuentaContable.AsNoTracking().ToListAsync();
        var porCodigo = new Dictionary<string, CuentaContable>();
        foreach (var req in CuentasRequeridas)
        {
            var hoja = cuentas.Where(c => c.Estado && c.Codigo.Length == 8 && c.Codigo.StartsWith(req)).OrderBy(c => c.Codigo).FirstOrDefault();
            if (hoja != null) porCodigo[req] = hoja;
        }
        var faltan = CuentasRequeridas.Where(c => !porCodigo.ContainsKey(c)).ToList();
        Item("Existe una cuenta de 8 dígitos (último nivel) bajo 88, 89, 4017, 591 y 592", faltan.Count == 0, "Faltan en el plan de cuentas: " + string.Join(", ", faltan));

        Plan? plan = null;
        string? errorPlan = null;
        if (v.Checklist.All(i => i.Ok))
            (plan, errorPlan) = await ArmarPlan(payload, cuentas, porCodigo);
        Item("Cuentas con saldo tienen Cuenta Cierre configurada", errorPlan == null, errorPlan);

        v.PuedeCerrar = v.Checklist.All(i => i.Ok);
        return (v, v.PuedeCerrar ? plan : null);
    }

    private async Task<(Plan?, string?)> ArmarPlan(CierreAnualPayload payload, List<CuentaContable> cuentas, Dictionary<string, CuentaContable> porCodigo)
    {
        var porId = cuentas.ToDictionary(c => c.Id);
        var inicio = new DateTime(payload.Anio, 1, 1);
        var fin = new DateTime(payload.Anio, 12, 31).AddDays(1);

        var filas = await _context.AsientoContableDetalle.AsNoTracking()
            .Where(d => d.AsientoContable!.EstadoAsiento == EstadoAsientoContable.Emitido && d.AsientoContable.Fecha < fin)
            .Select(d => new { d.CuentaContableId, d.Debe, d.Haber, d.AsientoContable!.Fecha }).ToListAsync();

        // Resultados (6, 7, 8, 9) cuentan solo el año; el balance (1 a 5) es acumulado.
        var saldos = new Dictionary<int, decimal>();
        foreach (var f in filas)
        {
            var cod = porId[f.CuentaContableId].Codigo;
            if (!EsBalance(cod) && f.Fecha < inicio) continue;
            saldos[f.CuentaContableId] = saldos.GetValueOrDefault(f.CuentaContableId) + f.Debe - f.Haber;
        }

        var fechaCierre = new DateTime(payload.Anio, 12, 31, 12, 0, 0);
        var asientos = new List<AsientoPlan>();
        string? error = null;

        // Anula cada cuenta de `ids` contra su destino y agrupa la contrapartida por cuenta destino.
        AsientoPlan? Transferir(int fase, string glosa, List<int> ids, Func<int, int?> destinoDe)
        {
            var lineas = new List<Linea>();
            var contra = new Dictionary<int, decimal>();
            foreach (var id in ids.Where(i => saldos.GetValueOrDefault(i) != 0).OrderBy(i => porId[i].Codigo))
            {
                var s = saldos[id];
                var destino = destinoDe(id);
                if (destino == null)
                {
                    error ??= $"Sin Cuenta Cierre: {porId[id].Codigo} {porId[id].Nombre}";
                    continue;
                }
                lineas.Add(s > 0 ? new Linea(id, 0, s) : new Linea(id, -s, 0));
                contra[destino.Value] = contra.GetValueOrDefault(destino.Value) + s;
                saldos[id] = 0;
            }
            foreach (var (destino, s) in contra)
            {
                lineas.Add(s > 0 ? new Linea(destino, s, 0) : new Linea(destino, 0, -s));
                saldos[destino] = saldos.GetValueOrDefault(destino) + s;
            }
            return lineas.Count == 0 ? null : new AsientoPlan(fase, OrigenCierre, fechaCierre, glosa, lineas);
        }

        int Id(string codigo) => porCodigo[codigo].Id;
        var idsDe = (Func<string, bool> filtro) => cuentas.Where(c => filtro(c.Codigo)).Select(c => c.Id).ToList();

        // Fase 2a/2b/2c: gestion -> clase 8, luego Impuesto a la Renta sobre el resultado.
        var gestion = idsDe(EsGestion);
        var resultadoAntes = -gestion.Sum(i => saldos.GetValueOrDefault(i));
        if (Transferir(2, $"Cierre {payload.Anio}: transferencia de clases 6, 7 y 9 a clase 8", gestion, i => porId[i].CuentaCierreId) is { } a2)
            asientos.Add(a2);

        var impuesto = resultadoAntes > 0 ? Math.Round(resultadoAntes * payload.TasaImpuestoRenta / 100, 2) : 0;
        if (impuesto > 0)
        {
            saldos[Id("88")] = saldos.GetValueOrDefault(Id("88")) + impuesto;
            saldos[Id("4017")] = saldos.GetValueOrDefault(Id("4017")) - impuesto;
            asientos.Add(new AsientoPlan(2, OrigenCierre, fechaCierre, $"Cierre {payload.Anio}: Impuesto a la Renta",
                new() { new Linea(Id("88"), impuesto, 0), new Linea(Id("4017"), 0, impuesto) }));
        }

        // Fase 3 (2d): clase 8 -> 89, deja 6, 7, 8 y 9 en cero.
        var c89 = Id("89");
        if (Transferir(3, $"Cierre {payload.Anio}: determinación del resultado del ejercicio (89)",
                idsDe(c => c[0] == '8' && !c.StartsWith("89")), _ => c89) is { } a3)
            asientos.Add(a3);

        // Fase 4: balance y resultado (89) contra su Cuenta Cierre. Foto previa para la apertura.
        var balance = idsDe(EsBalance);
        var apertura = balance.Where(i => saldos.GetValueOrDefault(i) != 0).Select(i => new Linea(i, saldos[i] > 0 ? saldos[i] : 0, saldos[i] < 0 ? -saldos[i] : 0)).ToList();
        var resultadoNeto = -saldos.GetValueOrDefault(c89);
        balance.Add(c89);
        if (Transferir(4, $"Cierre {payload.Anio}: cierre del Balance General", balance, i => porId[i].CuentaCierreId) is { } a4)
            asientos.Add(a4);

        // Fase 5: apertura del año siguiente, con el resultado trasladado a Resultados Acumulados.
        if (resultadoNeto > 0) apertura.Add(new Linea(Id("591"), 0, resultadoNeto));
        else if (resultadoNeto < 0) apertura.Add(new Linea(Id("592"), -resultadoNeto, 0));
        if (apertura.Count > 0)
            asientos.Add(new AsientoPlan(5, OrigenApertura, new DateTime(payload.Anio + 1, 1, 1), $"Asiento de apertura {payload.Anio + 1}", apertura));

        if (error != null) return (null, error);
        return (new Plan(asientos, resultadoAntes, impuesto), null);
    }
}
