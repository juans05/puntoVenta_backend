using Domain.Tenant;

namespace Application.Abstractions;

public interface ITenantContextAccessor
{
    TenantContext CurrentContext { get; }
}