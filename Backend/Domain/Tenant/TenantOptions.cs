namespace Domain.Tenant;

public class TenantOptions
{
    public string? DefaultConnection { get; set; }

    public Tenantx[] Tenants { get; set; } = Array.Empty<Tenantx>();
}