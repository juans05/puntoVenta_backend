using Domain.Entities;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Apps de la suite contratadas por empresa. Se lee en el login (claim "apps"), antes de que
// haya tenant en el contexto, por eso TenantApp no tiene query filter.
public class TenantAppRepository
{
    public const string Arp = "arp";
    public const string Arm = "arm";
    private static readonly string[] AppsValidas = { Arp, Arm };

    private readonly SpaContext _context;

    public TenantAppRepository(SpaContext context) => _context = context;

    public async Task<List<string>> AppsContratadas(int tenantIdentificador)
    {
        var filas = await _context.TenantApp.AsNoTracking()
            .Where(x => x.TenantIdentificador == tenantIdentificador).ToListAsync();

        return filas.Count == 0
            ? new List<string> { Arp }
            : filas.Where(x => x.Activo).Select(x => x.App).OrderBy(x => x).ToList();
    }

    public async Task<(ServiceStatus, string)> SetApp(int tenantIdentificador, string app, bool activo)
    {
        if (!AppsValidas.Contains(app))
            return (ServiceStatus.FailedValidation, $"App desconocida: {app}");

        if (!await _context.Tenant.IgnoreQueryFilters().AnyAsync(t => t.Identificador == tenantIdentificador))
            return (ServiceStatus.NotFound, "No existe el tenant");

        var filas = await _context.TenantApp.Where(x => x.TenantIdentificador == tenantIdentificador).ToListAsync();

        // Sin filas la empresa tenia "arp" implicito: se materializa para que tocar otra app no se lo quite.
        if (filas.Count == 0)
        {
            var arp = new TenantApp { TenantIdentificador = tenantIdentificador, App = Arp };
            _context.TenantApp.Add(arp);
            filas.Add(arp);
        }

        var fila = filas.FirstOrDefault(x => x.App == app);
        if (fila == null)
            _context.TenantApp.Add(new TenantApp { TenantIdentificador = tenantIdentificador, App = app, Activo = activo });
        else
            fila.Activo = activo;

        await _context.SaveChangesAsync();
        return (ServiceStatus.Ok, activo ? $"App {app} activada" : $"App {app} desactivada");
    }
}
