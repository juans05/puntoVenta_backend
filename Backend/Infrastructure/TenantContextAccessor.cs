using Application.Abstractions;
using Domain.Tenant;
using Microsoft.AspNetCore.Http;

namespace Infrastructure;

public class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext> _context = new();

    public TenantContext CurrentContext => _context.Value!;

    public static void Set(TenantContext context) => _context.Value = context;
}