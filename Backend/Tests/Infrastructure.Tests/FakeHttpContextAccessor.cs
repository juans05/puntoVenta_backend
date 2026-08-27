using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Tests;

/// <summary>
/// Minimal IHttpContextAccessor with a "username" claim, for repository
/// methods that read the logged-in user directly off HttpContext.
/// </summary>
public class FakeHttpContextAccessor : IHttpContextAccessor
{
    public FakeHttpContextAccessor(string username)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("username", username) }, "TestAuth");
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    public HttpContext? HttpContext { get; set; }
}
