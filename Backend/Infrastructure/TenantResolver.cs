using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Application.Abstractions;
using Domain.Models;
using Domain.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace Infrastructure;


public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IConfiguration _configuration;


    public TenantResolver(IHttpContextAccessor httpContextAccessor, ITenantRegistry tenantRegistry, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantRegistry = tenantRegistry;
        _configuration = configuration;
    }

    public Tenantx GetCurrentTenant()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var defeaultConnection = _configuration.GetSection("TenantOptions:DefaultConnection").Value;


            if (context == null)
            {
                return new Tenantx
                {
                    Name = "ADMIN",
                    ConnectionString = defeaultConnection
                };
            }

            var isLogin = context.Request.Path.Value.Contains("token");


            var isMaster = context.User.Identity.AuthenticationType == "ApiKey";

            if (isMaster)
                return new Tenantx
                {
                    Name = "ADMIN",
                    ConnectionString = defeaultConnection
                };

            var usuario = context.User.FindFirstValue("username");

            var tenantKey = context.User.FindFirstValue(ClaimConstants.TenantId);

            var origin = context.Request.Headers["Origin"].FirstOrDefault();

            // Capturar el host (esto incluye el subdominio)
            var host = context.Request.Host.Host;

            // Extraer el subdominio (si tu dominio es de tipo subdominio.dominio.com)
            var subdomain = GetSubdomain(host);

            // Puedes almacenar esta información en el contexto para usarla después
            context.Items["Origin"] = origin;
            context.Items["Subdomain"] = subdomain;

            var tenants = _tenantRegistry.GetTenants();

            // 1) Resolución por claim (usuario autenticado): el tenant viene en el JWT
            if (!string.IsNullOrEmpty(tenantKey))
            {
                var tenant = tenants.FirstOrDefault(t => t.Name == tenantKey);

                // Resolución por subdominio/header como respaldo al claim
                if (tenant is null)
                    tenant = ResolveTenant(tenants, tenantKey, subdomain);

                int? sucursalId = GetSucursalId(context, tenantKey, tenant);
                int? paisId = ParseInt(context.User.FindFirstValue(ClaimConstants.Pais));
                int? rubroId = ParseInt(context.User.FindFirstValue(ClaimConstants.Rubro));
                var monedaCodigo = context.User.FindFirstValue(ClaimConstants.Moneda);

                // Opción A (schema compartido): los tenants creados en BD usan la conexión por defecto.
                // Si el tenant declara su propia ConnectionString (Opción B), esta prevalece.
                return new Tenantx
                {
                    Username = usuario,
                    Name = tenant?.Name ?? tenantKey,
                    TenantKey = tenant?.TenantKey,
                    Subdomain = tenant?.Subdomain,
                    SucursalId = sucursalId,
                    RubroId = rubroId,
                    PaisId = paisId,
                    MonedaCodigo = monedaCodigo,
                    ConnectionString = tenant?.ConnectionString ?? defeaultConnection
                };
            }

            // 2) Resolución pre-autenticación (login / onboarding / branding) por subdominio o header
            var potentialName = isLogin ? null : userTenantHeader(context);
            var resolved = ResolveTenant(tenants, potentialName, subdomain);

            if (resolved is not null)
            {
                return new Tenantx
                {
                    Username = usuario,
                    Name = resolved.Name,
                    TenantKey = resolved.TenantKey,
                    Subdomain = resolved.Subdomain,
                    ConnectionString = resolved.ConnectionString
                };
            }

            // 3) Respaldo: tenant por defecto (1 sola BD compartida)
            return new Tenantx
            {
                Username = usuario,
                Name = tenantKey,
                ConnectionString = defeaultConnection
            };
        }
        catch (Exception ex)
        {
            throw new ErrorHandler(HttpStatusCode.InternalServerError, $"ERROR TENANT {ex.Message}", null, status: 500, internalResponse: 102);
        }
    }

    private static string? userTenantHeader(HttpContext context)
    {
        var header = context.Request.Headers["Tenant"].FirstOrDefault();

        if (string.IsNullOrEmpty(header)) return null;

        return header.ToUpper(); // TenantKey en BD es mayúscula para el default (SPASOLIS → SPASOLIS1)
    }

    private static int? GetSucursalId(HttpContext context, string tenantKey, Tenantx? tenant)
    {
        // El header X-Sucursal prevalece (permite cambiar de sede por request)
        var header = context.Request.Headers["X-Sucursal"].FirstOrDefault();
        if (int.TryParse(header, out var headerSucursal))
            return headerSucursal;

        var claim = context.User.FindFirstValue(ClaimConstants.Sucursal);
        if (int.TryParse(claim, out var claimSucursal))
            return claimSucursal;

        return null;
    }

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var result) ? result : null;

    private static Tenantx? ResolveTenant(Tenantx[] tenants, string? name, string? subdomain)
    {
        if (tenants == null || tenants.Length == 0) return null;

        if (!string.IsNullOrEmpty(subdomain))
        {
            var bySubdomain = tenants.FirstOrDefault(t =>
                string.Equals(t.Subdomain, subdomain, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.TenantKey, subdomain, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Name, subdomain, StringComparison.OrdinalIgnoreCase));

            if (bySubdomain != null) return bySubdomain;
        }

        if (!string.IsNullOrEmpty(name))
        {
            var byName = tenants.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.TenantKey, name, StringComparison.OrdinalIgnoreCase));

            if (byName != null) return byName;
        }

        return null;
    }
    private string GetSubdomain(string host)
    {
        // Suponiendo que tu dominio es "example.com" y esperas "sub.example.com"
        var domainParts = host.Split('.');
        if (domainParts.Length > 2)
        {
            return domainParts[0]; // Retorna el subdominio, ej. "sub"
        }
        return null; // No hay subdominio, solo el dominio principal
    }
}