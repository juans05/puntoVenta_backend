using Application.Abstractions;
using Domain.Tenant;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace API.Middlewares;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ITenantResolver tenantResolver)
    {
        var tenantx = tenantResolver.GetCurrentTenant();

        var tenantContext = new TenantContext
        {
            Name = tenantx.Name,
            TenantKey = tenantx.TenantKey,
            Subdomain = tenantx.Subdomain,
            Username = tenantx.Username,
            SucursalId = tenantx.SucursalId,
            RubroId = tenantx.RubroId,
            PaisId = tenantx.PaisId,
            MonedaCodigo = tenantx.MonedaCodigo,
            ConfiguracionFiscal = tenantx.ConfiguracionFiscal
        };

        TenantContextAccessor.Set(tenantContext);

        context.Items["TenantContext"] = tenantContext;

        await _next(context);
    }
}