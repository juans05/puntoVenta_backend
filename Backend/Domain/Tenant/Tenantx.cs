using Domain.Entities;

namespace Domain.Tenant;

public class Tenantx
{
    public string Name { get; set; } = null!;

    public string? TenantKey { get; set; }

    public string? Subdomain { get; set; }

    public string? ConnectionString { get; set; }

    public string? Username { get; set; }

    public int? SucursalId { get; set; }

    public int? RubroId { get; set; }

    public int? PaisId { get; set; }

    public string? MonedaCodigo { get; set; }

    public ConfiguracionFiscal? ConfiguracionFiscal { get; set; }
}