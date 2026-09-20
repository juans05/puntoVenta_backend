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
}
