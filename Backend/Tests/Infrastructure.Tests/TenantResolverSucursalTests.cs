using Domain.Tenant;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace Infrastructure.Tests;

public class TenantResolverSucursalTests
{
    private static HttpContext Contexto(string? claimSucursal, string? headerSucursal, params string[] roles)
    {
        var claims = new List<Claim>();
        if (claimSucursal != null) claims.Add(new Claim(ClaimConstants.Sucursal, claimSucursal));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        if (headerSucursal != null) ctx.Request.Headers["X-Sucursal"] = headerSucursal;
        return ctx;
    }

    [Fact]
    public void UsuarioSinSede_PuedeElegirSedeConElHeader()
        => Assert.Equal(7, TenantResolver.GetSucursalId(Contexto(null, "7"), "T", null));

    [Fact]
    public void UsuarioConSede_NoPuedeCambiarAOtraSedeConElHeader()
        => Assert.Equal(2, TenantResolver.GetSucursalId(Contexto("2", "9"), "T", null));

    [Fact]
    public void UsuarioConSede_ConHeaderIgualASuSede_Funciona()
        => Assert.Equal(2, TenantResolver.GetSucursalId(Contexto("2", "2"), "T", null));

    [Fact]
    public void Administrador_ConSede_PuedeCambiarDeSede()
        => Assert.Equal(9, TenantResolver.GetSucursalId(Contexto("2", "9", "Administrador"), "T", null));

    [Fact]
    public void SinHeader_UsaLaSedeDelToken()
        => Assert.Equal(2, TenantResolver.GetSucursalId(Contexto("2", null), "T", null));
}
