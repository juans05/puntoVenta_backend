using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WEB_API.Authorization;

namespace Infrastructure.Tests;

// Cubre el handler que hace de "piloto" del enforcement real de permisos (ver
// SubmoduloAuthorizationHandler): antes de esto, ningun endpoint del backend validaba el
// claim "rutas" del JWT, solo el Sidebar del frontend lo usaba para decidir que mostrar.
public class SubmoduloAuthorizationHandlerTests
{
    private static readonly string RutasConCatalogos =
        "[{\"Modulo\":\"1400\",\"ModuloNombre\":\"Configuración\",\"SubModulos\":[{\"SubModulo\":\"1401\",\"SubModuloNombre\":\"Catálogos de Documentos\"}]}]";

    private static readonly string RutasSinCatalogos =
        "[{\"Modulo\":\"300\",\"ModuloNombre\":\"Ventas del día\",\"SubModulos\":[{\"SubModulo\":\"301\",\"SubModuloNombre\":\"Ventas del día\"}]}]";

    private static async Task<AuthorizationHandlerContext> Evaluar(ClaimsPrincipal user)
    {
        var requirement = new SubmoduloRequirement("1401");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        await new SubmoduloAuthorizationHandler().HandleAsync(context);
        return context;
    }

    private static ClaimsPrincipal UsuarioCon(params Claim[] claims)
        => new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public async Task Usuario_con_submodulo_en_rutas_es_autorizado()
    {
        var user = UsuarioCon(new Claim("rutas", RutasConCatalogos));

        var context = await Evaluar(user);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Usuario_sin_el_submodulo_en_rutas_no_es_autorizado()
    {
        var user = UsuarioCon(new Claim("rutas", RutasSinCatalogos));

        var context = await Evaluar(user);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Usuario_sin_claim_rutas_no_es_autorizado()
    {
        var user = UsuarioCon();

        var context = await Evaluar(user);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Claim_rutas_malformado_no_es_autorizado_ni_revienta()
    {
        var user = UsuarioCon(new Claim("rutas", "{esto no es json valido"));

        var context = await Evaluar(user);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task SuperAdmin_es_autorizado_aunque_no_tenga_el_submodulo()
    {
        var user = UsuarioCon(new Claim(ClaimTypes.Role, "SuperAdmin"), new Claim("rutas", RutasSinCatalogos));

        var context = await Evaluar(user);

        Assert.True(context.HasSucceeded);
    }
}
