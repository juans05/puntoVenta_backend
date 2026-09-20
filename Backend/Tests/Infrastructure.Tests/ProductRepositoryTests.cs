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
            cfg.CreateMap<Producto, Producto>();
        });
        return config.CreateMapper();
    }

    private static async Task<(Moneda, TipoIgv, UnidadMedida, Sucursal, Categoria, Rubro)> SeedRequiredEntitiesAsync(SpaContext context)
    {
        var pais = new Pais
        {
            Id = 1,
            Codigo = "PE",
            Nombre = "Perú",
            Idioma = "es",
            MonedaCodigo = "PEN",
            TimeZone = "America/Lima",
            EsquemaFiscal = "UBL"
        };
        var moneda = new Moneda { Id = 1, Codigo = "PEN", Simbolo = "S/", Locale = "es-PE", PaisId = 1, Pais = pais };
        var tipoIgv = new TipoIgv { Id = 1, Codigo = "10", Descripcion = "Gravado - Operación Onerosa", AplicaPorcentajeImpuesto = true };
        var unidadMedida = new UnidadMedida { Id = 1, Codigo = "KGM", Descripcion = "Kilogramo" };
        var sucursal = new Sucursal { Id = 1, Nombre = "Tienda Principal", Direccion = "Calle Principal 123", MonedaId = 1, Moneda = moneda };
        var rubro = new Rubro { Id = 1, Nombre = "Bebidas" };
        var categoria = new Categoria { Id = 1, Nombre = "Bebidas", RubroId = 1, Rubro = rubro };

        context.Pais.Add(pais);
        context.Moneda.Add(moneda);
        context.TipoIgv.Add(tipoIgv);
        context.UnidadMedida.Add(unidadMedida);
        context.Sucursal.Add(sucursal);
        context.Rubro.Add(rubro);
        context.Categoria.Add(categoria);

        await context.SaveChangesAsync();

        return (moneda, tipoIgv, unidadMedida, sucursal, categoria, rubro);
    }

    [Fact]
    public async Task CreateProduct_GuardaTodosLosCampos()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: "ADMIN"));
        using var _ = connection;

        await SeedRequiredEntitiesAsync(context);

        var payload = new CreateProductPayload
        {
            Nombre = "Café Premium",
            Codigo = "CAFE-001",
            Marca = "Cafetal",
            CategoriaId = 1,
            SucursalId = 1,
            MonedaId = 1,
            TipoIgvId = 1,
            UnidadMedidaId = 1,
            PrecioVentaConInpuesto = 25.00m,
            Comentario = "Café importado de alta calidad",
            UsuarioCreacion = "ADMIN"
        };

        var mapper = GetMapper();
        var repository = new ProductRepository(context, mapper);
        var (estado, producto, mensaje) = await repository.CreateProduct(payload);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(producto);
        Assert.Equal("Café Premium", producto!.Nombre);
        Assert.Equal("CAFE-001", producto.Codigo);
        Assert.Equal("Cafetal", producto.Marca);
        Assert.Equal(1, producto.CategoriaId);
        Assert.Equal(1, producto.SucursalId);
        Assert.Equal(1, producto.MonedaId);
        Assert.Equal(1, producto.TipoIgvId);
        Assert.Equal(1, producto.UnidadMedidaId);
        Assert.Equal(25.00m, producto.PrecioVentaConInpuesto);
        Assert.Equal("Café importado de alta calidad", producto.Comentario);
    }

    [Fact]
    public async Task UpdateProduct_ActualizaTodosLosCampos()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: "ADMIN"));
        using var _ = connection;

        var (moneda, tipoIgv, unidadMedida, sucursal, categoria, rubro) = await SeedRequiredEntitiesAsync(context);

        // Crear producto inicial
        var productoInicial = new Producto
        {
            Nombre = "Café Básico",
            Codigo = "CAFE-001",
            Marca = "CaféCo",
            CategoriaId = 1,
            SucursalId = 1,
            MonedaId = 1,
            TipoIgvId = 1,
            UnidadMedidaId = 1,
            PrecioVentaConInpuesto = 15.00m,
            Comentario = "Café estándar",
            Stock = 100
        };
        context.Producto.Add(productoInicial);
        await context.SaveChangesAsync();

        // Actualizar con nuevos valores
        var payload = new UpdateProductPayload
        {
            ProductoId = productoInicial.Id,
            Nombre = "Café Premium Actualizado",
            Codigo = "CAFE-PREMIUM",
            Marca = "Cafetal Importado",
            CategoriaId = 1,
            SucursalId = 1,
            MonedaId = 1,
            TipoIgvId = 1,
            UnidadMedidaId = 1,
            PrecioVentaConInpuesto = 35.00m,
            Comentario = "Café premium importado actualizado",
            Stock = 100,
            UsuarioModificacion = "ADMIN"
        };

        var mapper = GetMapper();
        var repository = new ProductRepository(context, mapper);
        var (estado, productoActualizado, mensaje) = await repository.UpdateProduct(payload);

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(productoActualizado);
        Assert.Equal("Café Premium Actualizado", productoActualizado!.Nombre);
        Assert.Equal("CAFE-PREMIUM", productoActualizado.Codigo);
        Assert.Equal("Cafetal Importado", productoActualizado.Marca);
        Assert.Equal(1, productoActualizado.CategoriaId);
        Assert.Equal(1, productoActualizado.TipoIgvId);
        Assert.Equal(1, productoActualizado.UnidadMedidaId);
        Assert.Equal(35.00m, productoActualizado.PrecioVentaConInpuesto);
        Assert.Equal("Café premium importado actualizado", productoActualizado.Comentario);
    }
}
