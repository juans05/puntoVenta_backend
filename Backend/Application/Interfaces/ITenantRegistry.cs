using Domain.Tenant;

namespace Application.Abstractions;

public interface ITenantRegistry
{
    Tenantx[] GetTenants();
}