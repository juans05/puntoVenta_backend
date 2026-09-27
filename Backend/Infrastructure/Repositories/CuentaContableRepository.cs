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

    private static CuentaContableDto ToDto(CuentaContable c) => new()
    {
        Id = c.Id,
        Codigo = c.Codigo,
        Nombre = c.Nombre,
        Tipo = c.Tipo,
        CuentaPadreId = c.CuentaPadreId,
        Estado = c.Estado
    };

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
        _context.CuentaContable.Add(cuenta);
        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, ToDto(cuenta), "Success");
    }

    public async Task<(ServiceStatus, CuentaContableDto?, string)> Actualizar(int id, ActualizarCuentaContablePayload payload)
    {
        if (await Validar(payload, id) is { } error)
            return (ServiceStatus.FailedValidation, null, error);

        var cuenta = await _context.CuentaContable.AsTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cuenta == null) return (ServiceStatus.NotFound, null, "Cuenta no encontrada");
        if (payload.CuentaPadreId == id) return (ServiceStatus.FailedValidation, null, "Una cuenta no puede ser su propia cuenta padre");

        cuenta.Codigo = payload.Codigo.Trim();
        cuenta.Nombre = payload.Nombre.Trim();
        cuenta.Tipo = payload.Tipo;
        cuenta.CuentaPadreId = payload.CuentaPadreId;
        cuenta.Estado = payload.Estado;
        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, ToDto(cuenta), "Success");
    }

    public async Task<(ServiceStatus, List<CuentaContableDto>?, string)> Listar(bool incluirInactivas = false)
    {
        var query = _context.CuentaContable.AsNoTracking();
        if (!incluirInactivas) query = query.Where(c => c.Estado);
        var cuentas = await query.OrderBy(c => c.Codigo).ToListAsync();
        return (ServiceStatus.Ok, cuentas.Select(ToDto).ToList(), "Success");
    }

    public async Task<(ServiceStatus, CuentaContableDto?, string)> Obtener(int id)
    {
        var cuenta = await _context.CuentaContable.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return cuenta == null
            ? (ServiceStatus.NotFound, null, "Cuenta no encontrada")
            : (ServiceStatus.Ok, ToDto(cuenta), "Success");
    }

    public async Task<CuentaContableDto?> ObtenerPorCodigo(string codigo)
    {
        var cuenta = await _context.CuentaContable.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo);
        return cuenta == null ? null : ToDto(cuenta);
    }
}
