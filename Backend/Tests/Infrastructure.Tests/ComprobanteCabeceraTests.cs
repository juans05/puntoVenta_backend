using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Xunit;

namespace Infrastructure.Tests;

public class ComprobanteCabeceraTests
{
    private const string Usuario = "VENDEDOR1";

    private static async Task<(TipoDocumentoVenta, Metodopago)> SeedRequiredEntitiesAsync(SpaContext context)
    {
        var tipoDoc = new TipoDocumentoVenta { Nombre = "Boleta" };
        var metodo = new Metodopago { Nombre = "Efectivo" };

        context.TipoDocumentoVenta.Add(tipoDoc);
        context.Metodopago.Add(metodo);
        await context.SaveChangesAsync();

        return (tipoDoc, metodo);
    }

    [Fact]
    public async Task CrearComprobante_GuardaSerieYCorrelativo()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: Usuario));
        using var _ = connection;

        var (tipoDoc, metodo) = await SeedRequiredEntitiesAsync(context);

        var comprobante = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = tipoDoc.Id,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 100m,
            EstadoComprobante = EstatusComprobante.Creado
        };

        context.ComprobanteCabecera.Add(comprobante);
        await context.SaveChangesAsync();

        var savedComprobante = await context.ComprobanteCabecera
            .Where(c => c.Id == comprobante.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedComprobante);
        Assert.Equal("B001", savedComprobante.Serie);
        Assert.Equal(1, savedComprobante.Correlativo);
        Assert.Equal(100m, savedComprobante.ValorTotal);
    }

    [Fact]
    public async Task AgregarPago_GuardaMontoyMetodo()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: Usuario));
        using var _ = connection;

        var (tipoDoc, metodo) = await SeedRequiredEntitiesAsync(context);

        // Crear comprobante
        var comprobante = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = tipoDoc.Id,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 100m,
            EstadoComprobante = EstatusComprobante.Creado
        };
        context.ComprobanteCabecera.Add(comprobante);
        await context.SaveChangesAsync();

        // Agregar pago
        var pago = new Pago
        {
            ComprobanteCabeceraId = comprobante.Id,
            MetodoPagoId = metodo.Id,
            Monto = 100m
        };
        context.Pago.Add(pago);
        await context.SaveChangesAsync();

        var savedPago = await context.Pago
            .Where(p => p.ComprobanteCabeceraId == comprobante.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedPago);
        Assert.Equal(comprobante.Id, savedPago.ComprobanteCabeceraId);
        Assert.Equal(metodo.Id, savedPago.MetodoPagoId);
        Assert.Equal(100m, savedPago.Monto);
    }

    [Fact]
    public async Task ComprobanteDetalle_GuardaProductoYCantidad()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: Usuario));
        using var _ = connection;

        var (tipoDoc, metodo) = await SeedRequiredEntitiesAsync(context);

        // Crear producto
        var producto = new Producto
        {
            Nombre = "Producto Test",
            Precio = 50m,
            Stock = 100,
            RestriccionEdad = 0
        };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();

        // Crear comprobante
        var comprobante = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = tipoDoc.Id,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 100m,
            EstadoComprobante = EstatusComprobante.Creado
        };
        context.ComprobanteCabecera.Add(comprobante);
        await context.SaveChangesAsync();

        // Agregar detalle
        var detalle = new ComprobanteDetalle
        {
            ComprobanteCabeceraId = comprobante.Id,
            ProductoId = producto.Id,
            Cantidad = 2,
            ValorUnitario = 50m,
            ValorUnitarioTotal = 100m,
            ValorIgv = 18m
        };
        context.ComprobanteDetalle.Add(detalle);
        await context.SaveChangesAsync();

        var savedDetalle = await context.ComprobanteDetalle
            .Where(d => d.ComprobanteCabeceraId == comprobante.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedDetalle);
        Assert.Equal(comprobante.Id, savedDetalle.ComprobanteCabeceraId);
        Assert.Equal(producto.Id, savedDetalle.ProductoId);
        Assert.Equal(2, savedDetalle.Cantidad);
        Assert.Equal(50m, savedDetalle.ValorUnitario);
    }

    [Fact]
    public async Task ComprobanteAnulado_CambiaEstado()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: Usuario));
        using var _ = connection;

        var (tipoDoc, metodo) = await SeedRequiredEntitiesAsync(context);

        // Crear comprobante
        var comprobante = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = tipoDoc.Id,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 100m,
            EstadoComprobante = EstatusComprobante.Creado
        };
        context.ComprobanteCabecera.Add(comprobante);
        await context.SaveChangesAsync();

        // Anular
        var comprobanteParaAnular = await context.ComprobanteCabecera
            .Where(c => c.Id == comprobante.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(comprobanteParaAnular);
        comprobanteParaAnular.EstadoComprobante = EstatusComprobante.Anulado;
        context.ComprobanteCabecera.Update(comprobanteParaAnular);
        await context.SaveChangesAsync();

        var anulado = await context.ComprobanteCabecera
            .Where(c => c.Id == comprobante.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(anulado);
        Assert.Equal(EstatusComprobante.Anulado, anulado.EstadoComprobante);
    }
}
