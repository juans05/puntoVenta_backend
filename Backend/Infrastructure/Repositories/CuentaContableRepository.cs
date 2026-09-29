using Application.Interfaces.IRepository;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Plan de cuentas (PCGE), editable por tenant. El catalogo se siembra al arrancar desde
// cuentacontable.json (ver PuntoVentaDbContextData.SeedCatalogoTenant); este repo solo
// administra el CRUD sobre lo ya sembrado (agregar/editar/desactivar cuentas propias).
public class CuentaContableRepository : ICuentaContableRepository
{
    private static readonly string[] TiposValidos =
    {
        TipoCuentaContable.Activo, TipoCuentaContable.Pasivo, TipoCuentaContable.Patrimonio,
        TipoCuentaContable.Ingreso, TipoCuentaContable.Gasto
    };

    private readonly SpaContext _context;

    public CuentaContableRepository(SpaContext context)
    {
        _context = context;
    }

    private IQueryable<CuentaContable> Query() => _context.CuentaContable.AsNoTracking()
        .Include(c => c.CentroCosto)
        .Include(c => c.CuentaCargo1).Include(c => c.CuentaAbono1)
        .Include(c => c.CuentaCargo2).Include(c => c.CuentaAbono2)
        .Include(c => c.CuentaCargo3).Include(c => c.CuentaAbono3)
        .Include(c => c.CuentaCierre);

    private static string? Etiqueta(CuentaContable? c) => c == null ? null : $"{c.Codigo} - {c.Nombre}";

    private static CuentaContableDto ToDto(CuentaContable c) => new()
    {
        Id = c.Id,
        Codigo = c.Codigo,
        Nombre = c.Nombre,
        Tipo = c.Tipo,
        CuentaPadreId = c.CuentaPadreId,
        Estado = c.Estado,
        Nivel = c.Nivel,
        ClaseCuenta = c.ClaseCuenta,
        TipoAnexo = c.TipoAnexo,
        CuentaMonetaria = c.CuentaMonetaria,
        AjusteDifCambio = c.AjusteDifCambio,
        CodigoEeff = c.CodigoEeff,
        CodigoEeffTributario = c.CodigoEeffTributario,
        CodigoEeffNiif = c.CodigoEeffNiif,
        ClasificacionBienServicio = c.ClasificacionBienServicio,
        Destino = c.Destino,
        CentroCostoId = c.CentroCostoId,
        CentroCosto = c.CentroCosto?.Nombre,
        CuentaCargo1Id = c.CuentaCargo1Id,
        CuentaCargo1 = Etiqueta(c.CuentaCargo1),
        CuentaAbono1Id = c.CuentaAbono1Id,
        CuentaAbono1 = Etiqueta(c.CuentaAbono1),
        PorcentajeDestino1 = c.PorcentajeDestino1,
        CuentaCargo2Id = c.CuentaCargo2Id,
        CuentaCargo2 = Etiqueta(c.CuentaCargo2),
        CuentaAbono2Id = c.CuentaAbono2Id,
        CuentaAbono2 = Etiqueta(c.CuentaAbono2),
        PorcentajeDestino2 = c.PorcentajeDestino2,
        CuentaCargo3Id = c.CuentaCargo3Id,
        CuentaCargo3 = Etiqueta(c.CuentaCargo3),
        CuentaAbono3Id = c.CuentaAbono3Id,
        CuentaAbono3 = Etiqueta(c.CuentaAbono3),
        PorcentajeDestino3 = c.PorcentajeDestino3,
        CuentaCierreId = c.CuentaCierreId,
        CuentaCierre = Etiqueta(c.CuentaCierre)
    };

    private static void AplicarCampos(CuentaContable cuenta, CrearCuentaContablePayload payload)
    {
        cuenta.Nivel = payload.Nivel;
        cuenta.ClaseCuenta = payload.ClaseCuenta?.Trim();
        cuenta.TipoAnexo = payload.TipoAnexo;
        cuenta.CuentaMonetaria = payload.CuentaMonetaria;
        cuenta.AjusteDifCambio = payload.AjusteDifCambio;
        cuenta.CodigoEeff = payload.CodigoEeff?.Trim();
        cuenta.CodigoEeffTributario = payload.CodigoEeffTributario?.Trim();
        cuenta.CodigoEeffNiif = payload.CodigoEeffNiif?.Trim();
        cuenta.ClasificacionBienServicio = payload.ClasificacionBienServicio?.Trim();
        cuenta.Destino = payload.Destino;
        cuenta.CentroCostoId = payload.CentroCostoId;
        cuenta.CuentaCargo1Id = payload.CuentaCargo1Id;
        cuenta.CuentaAbono1Id = payload.CuentaAbono1Id;
        cuenta.PorcentajeDestino1 = payload.PorcentajeDestino1;
        cuenta.CuentaCargo2Id = payload.CuentaCargo2Id;
        cuenta.CuentaAbono2Id = payload.CuentaAbono2Id;
        cuenta.PorcentajeDestino2 = payload.PorcentajeDestino2;
        cuenta.CuentaCargo3Id = payload.CuentaCargo3Id;
        cuenta.CuentaAbono3Id = payload.CuentaAbono3Id;
        cuenta.PorcentajeDestino3 = payload.PorcentajeDestino3;
        cuenta.CuentaCierreId = payload.CuentaCierreId;
    }

    // Las 7 referencias a otra cuenta (Cargo/Abono x3 + Cierre) deben existir; se validan todas
    // juntas en una sola consulta en vez de un roundtrip por campo.
    private async Task<string?> ValidarCuentasReferenciadas(CrearCuentaContablePayload payload)
    {
        var ids = new[]
        {
            payload.CuentaCargo1Id, payload.CuentaAbono1Id, payload.CuentaCargo2Id, payload.CuentaAbono2Id,
            payload.CuentaCargo3Id, payload.CuentaAbono3Id, payload.CuentaCierreId
        }.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0) return null;

        var existentes = await _context.CuentaContable.AsNoTracking().Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync();
        if (existentes.Count != ids.Count) return "Una de las cuentas de Cargo/Abono/Cierre elegidas no existe";

        if (payload.CentroCostoId != null && !await _context.CentroCosto.AsNoTracking().AnyAsync(c => c.Id == payload.CentroCostoId))
            return "El centro de costo elegido no existe";
        return null;
    }

    private async Task<string?> Validar(CrearCuentaContablePayload payload, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(payload.Codigo)) return "El código es obligatorio";
        if (string.IsNullOrWhiteSpace(payload.Nombre)) return "El nombre es obligatorio";
        if (!TiposValidos.Contains(payload.Tipo)) return "Tipo de cuenta inválido";
        if (await _context.CuentaContable.AsNoTracking()
                .AnyAsync(c => c.Codigo == payload.Codigo.Trim() && c.Id != idActual))
            return "Ya existe una cuenta con ese código";
        if (payload.CuentaPadreId != null &&
            !await _context.CuentaContable.AsNoTracking().AnyAsync(c => c.Id == payload.CuentaPadreId))
            return "La cuenta padre elegida no existe";
        if (await ValidarCuentasReferenciadas(payload) is { } errorRef) return errorRef;
        return null;
    }

    public async Task<(ServiceStatus, CuentaContableDto?, string)> Crear(CrearCuentaContablePayload payload)
    {
        if (await Validar(payload, null) is { } error)
            return (ServiceStatus.FailedValidation, null, error);

        var cuenta = new CuentaContable
        {
            Codigo = payload.Codigo.Trim(),
            Nombre = payload.Nombre.Trim(),
            Tipo = payload.Tipo,
            CuentaPadreId = payload.CuentaPadreId
        };
        AplicarCampos(cuenta, payload);
        _context.CuentaContable.Add(cuenta);
        await _context.SaveChangesAsync();

        return await Obtener(cuenta.Id);
    }

    public async Task<(ServiceStatus, CuentaContableDto?, string)> Actualizar(int id, ActualizarCuentaContablePayload payload)
    {
        if (await Validar(payload, id) is { } error)
            return (ServiceStatus.FailedValidation, null, error);

        var cuenta = await _context.CuentaContable.AsTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cuenta == null) return (ServiceStatus.NotFound, null, "Cuenta no encontrada");
        if (payload.CuentaPadreId != null && await FormaCiclo(id, payload.CuentaPadreId.Value))
            return (ServiceStatus.FailedValidation, null, "Esa cuenta padre generaría un ciclo (una cuenta no puede descender de sí misma)");

        cuenta.Codigo = payload.Codigo.Trim();
        cuenta.Nombre = payload.Nombre.Trim();
        cuenta.Tipo = payload.Tipo;
        cuenta.CuentaPadreId = payload.CuentaPadreId;
        cuenta.Estado = payload.Estado;
        AplicarCampos(cuenta, payload);
        await _context.SaveChangesAsync();

        return await Obtener(id);
    }

    // Sube por la cadena de padres desde nuevoPadreId; si encuentra id en el camino, asignarlo
    // como padre crearia un ciclo (directo -- nuevoPadreId == id -- o indirecto via un ancestro).
    // Tope de 100 saltos: por si ya hubiera un ciclo preexistente en los datos, no nos quedamos
    // dando vueltas infinitas.
    private async Task<bool> FormaCiclo(int id, int nuevoPadreId)
    {
        var actual = (int?)nuevoPadreId;
        for (var i = 0; i < 100 && actual != null; i++)
        {
            if (actual == id) return true;
            actual = await _context.CuentaContable.AsNoTracking()
                .Where(c => c.Id == actual).Select(c => c.CuentaPadreId).FirstOrDefaultAsync();
        }
        return false;
    }

    public async Task<(ServiceStatus, List<CuentaContableDto>?, string)> Listar(bool incluirInactivas = false)
    {
        var query = Query();
        if (!incluirInactivas) query = query.Where(c => c.Estado);
        var cuentas = await query.OrderBy(c => c.Codigo).ToListAsync();
        return (ServiceStatus.Ok, cuentas.Select(ToDto).ToList(), "Success");
    }

    public async Task<(ServiceStatus, CuentaContableDto?, string)> Obtener(int id)
    {
        var cuenta = await Query().FirstOrDefaultAsync(c => c.Id == id);
        return cuenta == null
            ? (ServiceStatus.NotFound, null, "Cuenta no encontrada")
            : (ServiceStatus.Ok, ToDto(cuenta), "Success");
    }

    public async Task<CuentaContableDto?> ObtenerPorCodigo(string codigo)
    {
        var cuenta = await Query().FirstOrDefaultAsync(c => c.Codigo == codigo);
        return cuenta == null ? null : ToDto(cuenta);
    }
}
