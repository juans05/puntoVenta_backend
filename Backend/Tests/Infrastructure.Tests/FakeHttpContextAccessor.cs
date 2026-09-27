using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Tests;

/// <summary>
/// Minimal IHttpContextAccessor with a "username" claim, for repository
/// methods that read the logged-in user directly off HttpContext.
/// </summary>
public class FakeHttpContextAccessor : IHttpContextAccessor
{
    // userId/roles: usados por OrdenCompraRepository para resolver el aprobador asignado
    // (ClaimTypes.NameIdentifier) y el respaldo de administrador (IsInRole).
    public FakeHttpContextAccessor(string username, string? userId = null, params string[] roles)
    {
        var claims = new List<Claim> { new("username", username) };
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    public HttpContext? HttpContext { get; set; }
}
