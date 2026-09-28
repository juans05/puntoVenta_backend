using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Infrastructure.Repositories;
using AutoMapper;
using Xunit;

namespace Infrastructure.Tests;

public class ProductRepositoryTests
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.CreateMap<CreateProductPayload, Producto>();
            cfg.CreateMap<UpdateProductPayload, Producto>();
        });
        return config.CreateMapper();
    }

    [Fact]
    public async Task CreateProduct_GuardaTodosLosCampos()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: "ADMIN"));
        using var _ = connection;

        var payload = new CreateProductPayload
        {
            Nombre = "Café Premium",
            Codigo = "CAFE-001",
            Marca = "Cafetal",
            PrecioVentaConInpuesto = 25.00m,
            Comentario = "Café importado de alta calidad",
            UsuarioCreacion = "ADMIN"
        };

        var mapper = GetMapper();
        var repository = new ProductRepository(context, mapper);
        var (estado, producto, _) = await repository.CreateProduct(payload);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(producto);
        Assert.Equal("Café Premium", producto!.Nombre);
        Assert.Equal("CAFE-001", producto.Codigo);
        Assert.Equal("Cafetal", producto.Marca);
        Assert.Equal(25.00m, producto.PrecioVentaConInpuesto);
        Assert.Equal("Café importado de alta calidad", producto.Comentario);
    }

    [Fact]
    public async Task GetProducto_FiltraPorSeVendeYSeCompra()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: "ADMIN"));
        using var _ = connection;
        context.Producto.AddRange(
            new Producto { Nombre = "Solo se vende", SeVende = true, SeCompra = false, RestriccionEdad = 0 },
            new Producto { Nombre = "Solo se compra", SeVende = false, SeCompra = true, RestriccionEdad = 0 },
            new Producto { Nombre = "Ambos", SeVende = true, SeCompra = true, RestriccionEdad = 0 });
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context, TestDbContextFactory.Mapper);

        var (_, soloVenta, _) = await repository.GetProducto(new ProductPayload { SeVende = true, Page = 1, Amount = 10 });
        Assert.Equal(new[] { "Ambos", "Solo se vende" }, soloVenta!.Items!.Select(p => p.Nombre).OrderBy(n => n));

        var (_, soloCompra, _) = await repository.GetProducto(new ProductPayload { SeCompra = true, Page = 1, Amount = 10 });
        Assert.Equal(new[] { "Ambos", "Solo se compra" }, soloCompra!.Items!.Select(p => p.Nombre).OrderBy(n => n));
    }
}
