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

        Assert.NotNull(await repo.GuardarConfig(new TiendaConfig { Titulo = "T", ColorPrimario = "rojo", ColorFondo = "#ffffff", ColorTexto = "#000000" }));
    }
}
