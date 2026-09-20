using Domain.Entities;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class CategoriaTests
{
    [Fact]
    public async Task CrearCategoria_GuardaNombreYRubro()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: "ADMIN"));
        using var _ = connection;

        var rubro = new Rubro { Nombre = "Alimentos" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var categoria = new Categoria
        {
            Nombre = "Bebidas",
            RubroId = rubro.Id
        };

        context.Categoria.Add(categoria);
        await context.SaveChangesAsync();

        var saved = await context.Categoria
            .Where(c => c.Id == categoria.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(saved);
        Assert.Equal("Bebidas", saved.Nombre);
        Assert.Equal(rubro.Id, saved.RubroId);
    }

    [Fact]
    public async Task ActualizarCategoria_CambiaName()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: "ADMIN"));
        using var _ = connection;

        var rubro = new Rubro { Nombre = "Alimentos" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var categoria = new Categoria
        {
            Nombre = "Bebidas",
            RubroId = rubro.Id
        };
        context.Categoria.Add(categoria);
        await context.SaveChangesAsync();

        var paraActualizar = await context.Categoria
            .Where(c => c.Id == categoria.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(paraActualizar);
        paraActualizar.Nombre = "Bebidas Frías";
        context.Categoria.Update(paraActualizar);
        await context.SaveChangesAsync();

        var updated = await context.Categoria
            .Where(c => c.Id == categoria.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(updated);
        Assert.Equal("Bebidas Frías", updated.Nombre);
    }

    [Fact]
    public async Task ListarCategorias_RetornaTodasLasCategoras()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: "ADMIN"));
        using var _ = connection;

        var rubro = new Rubro { Nombre = "Alimentos" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var cat1 = new Categoria { Nombre = "Bebidas", RubroId = rubro.Id };
        var cat2 = new Categoria { Nombre = "Snacks", RubroId = rubro.Id };

        context.Categoria.AddRange(cat1, cat2);
        await context.SaveChangesAsync();

        var todas = await context.Categoria
            .Where(c => c.RubroId == rubro.Id)
            .ToListAsync();

        Assert.NotEmpty(todas);
        Assert.True(todas.Count >= 2);
    }
}
