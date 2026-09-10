using Application.Abstractions;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Infrastructure.Tests")]

namespace Infrastructure;

public class TenantRegistry : ITenantRegistry
{
    private readonly string _defaultConnection;

    public TenantRegistry(IConfiguration configuration)
    {
        _defaultConnection = configuration.GetSection("TenantOptions").GetValue<string>("DefaultConnection")
            ?? configuration.GetValue<string>("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró TenantOptions:DefaultConnection en la configuración.");
    }

    public Tenantx[] GetTenants()
    {
        var options = new DbContextOptionsBuilder<SpaContext>().UseNpgsql(_defaultConnection).Options;
        var bootstrap = new Tenantx { Name = "public", ConnectionString = _defaultConnection };
        using var context = new SpaContext(options, new BootstrapTenantResolver(bootstrap));
        return GetActiveTenants(context);
    }

    /// <summary>
    /// Query logic factored out from GetTenants() so it can be unit-tested against
    /// the SQLite in-memory SpaContext (TestDbContextFactory) without a real Postgres
    /// connection. Tenant/Empresa carry no HasQueryFilter (see SpaContext.OnModelCreating),
    /// so this reads every tenant regardless of which tenant "context" was bootstrapped with.
    /// </summary>
    internal static Tenantx[] GetActiveTenants(SpaContext context)
    {
        return context.Tenant
            .Include(t => t.Moneda)
            .Where(t => t.Activo)
            .Select(t => new Tenantx
            {
                Name = t.Name,
                TenantKey = t.TenantKey,
                RubroId = t.RubroId,
                PaisId = t.PaisId,
                MonedaCodigo = t.Moneda != null ? t.Moneda.Codigo : null,
            })
            .ToArray();
    }

    private class BootstrapTenantResolver : ITenantResolver
    {
        private readonly Tenantx _tenant;
        public BootstrapTenantResolver(Tenantx tenant) => _tenant = tenant;
        public Tenantx GetCurrentTenant() => _tenant;
    }
}