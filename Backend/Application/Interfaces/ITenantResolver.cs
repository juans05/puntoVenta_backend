using Domain.Tenant;

namespace Application.Abstractions;

public interface ITenantResolver
{
    Tenantx GetCurrentTenant();
}