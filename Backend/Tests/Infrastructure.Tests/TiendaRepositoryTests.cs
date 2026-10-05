using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class TiendaRepositoryTests
{
    [Fact]
    public async Task ObtenerPublica_SoloSiPublicada_YSoloProductosDelTenant()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection; using var __ = context;
        var repo = new TiendaRepository(context);

        // El enlace publico lleva el TenantKey; los datos se guardan con TenantId = Tenant.Name.
        context.Tenant.Add(new Tenant { Name = "TEST", TenantKey = "tendy-key", RubroId = 1 });
        context.Producto.Add(new Producto { Nombre = "Propio", Precio = 10, Stock = 5, TenantId = "TEST" });
        context.Producto.Add(new Producto { Nombre = "NoSeVende", Precio = 10, SeVende = false, TenantId = "TEST" });
        context.Producto.Add(new Producto { Nombre = "Ajeno", Precio = 10, TenantId = "OTRO" });
        await context.SaveChangesAsync();
        // SaveChanges fuerza TenantId al del contexto en altas: el producto ajeno se reasigna despues.
        context.Producto.AsTracking().Single(p => p.Nombre == "Ajeno").TenantId = "OTRO";
        await context.SaveChangesAsync();

        Assert.Null(await repo.ObtenerPublica("test")); // sin config / sin publicar -> 404

        Assert.Null(await repo.GuardarConfig(new TiendaConfig { Publicada = true, Titulo = "T", ColorPrimario = "#112233", ColorFondo = "#ffffff", ColorTexto = "#000000" }));
        // Tipo anonimo (internal): se inspecciona via JSON, no con dynamic.
        var json = Newtonsoft.Json.Linq.JObject.FromObject((await repo.ObtenerPublica("test"))!);
        Assert.Equal(new[] { "Propio" }, json["productos"]!.Select(p => (string)p["Nombre"]!).ToArray());
        Assert.NotNull(await repo.ObtenerPublica("Tendy-Key")); // por TenantKey, sin distinguir mayusculas
        Assert.Null(await repo.ObtenerPublica("inexistente"));

        var ok = new TiendaConfig { Titulo = "T", ColorPrimario = "#112233", ColorFondo = "#ffffff", ColorTexto = "#000000" };
        ok.LogoUrl = "javascript:alert(1)";
        Assert.NotNull(await repo.GuardarConfig(ok));
        ok.LogoUrl = "https://x.com/logo.png"; ok.Whatsapp = "51999999999";
        Assert.Null(await repo.GuardarConfig(ok));
        Assert.NotNull(await repo.GuardarConfig(new TiendaConfig { Titulo = "T", ColorPrimario = "rojo", ColorFondo = "#ffffff", ColorTexto = "#000000" }));
    }

    [Fact]
    public async Task ObtenerProductoPublico_SoloDisponibles_ConRelacionados()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection; using var __ = context;
        var repo = new TiendaRepository(context);

        context.Tenant.Add(new Tenant { Name = "TEST", TenantKey = "tendy-key", RubroId = 1 });
        context.Producto.AddRange(
            new Producto { Nombre = "A", Precio = 10, Stock = 5 },
            new Producto { Nombre = "B", Precio = 20, Stock = 3 },
            new Producto { Nombre = "Agotado", Precio = 5, Stock = 0 });
        await context.SaveChangesAsync();
        var ids = context.Producto.ToDictionary(p => p.Nombre, p => p.Id);

        Assert.Null(await repo.ObtenerProductoPublico("test", ids["A"])); // tienda sin publicar
        Assert.Null(await repo.GuardarConfig(new TiendaConfig { Publicada = true, Titulo = "T", ColorPrimario = "#112233", ColorFondo = "#ffffff", ColorTexto = "#000000" }));

        var json = Newtonsoft.Json.Linq.JObject.FromObject((await repo.ObtenerProductoPublico("tendy-key", ids["A"]))!);
        Assert.Equal("A", (string)json["producto"]!["Nombre"]!);
        Assert.Equal(new[] { "B" }, json["relacionados"]!.Select(p => (string)p["Nombre"]!).ToArray());
        Assert.Null(await repo.ObtenerProductoPublico("test", ids["Agotado"])); // agotado -> 404
        Assert.Null(await repo.ObtenerProductoPublico("test", 99999));
    }
}
