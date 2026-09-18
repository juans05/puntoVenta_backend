using Domain.Common;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class ComprobanteRepositoryTests
{
    private static async Task<int> SeedProductoAsync(Infrastructure.Data.SpaContext context, int stock)
    {
        var producto = new Producto { Nombre = "Producto test", Precio = 10m, Stock = stock, RestriccionEdad = 0 };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();
        return producto.Id;
    }

    private static async Task SeedTipoDocumentoVentaAsync(Infrastructure.Data.SpaContext context)
    {
        // Catalogo global (sin query filter): sembrado directo por Id, como hace el seed real.
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 1))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 1, Nombre = "Factura" });
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 2))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 2, Nombre = "Boleta" });
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 3))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 3, Nombre = "Nota de Venta" });
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 4))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 4, Nombre = "Nota de Credito" });
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 5))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 5, Nombre = "Nota de Debito" });
        if (!await context.TipoDocumentoVenta.AnyAsync(x => x.Id == 6))
            context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 6, Nombre = "Cotizacion" });
        await context.SaveChangesAsync();
    }

    private static async Task<int> SeedMotivoNotaAsync(Infrastructure.Data.SpaContext context, int tipoDocumentoVentaId, bool revierteStock)
    {
        var motivo = new MotivoNota
        {
            TipoDocumentoVentaId = tipoDocumentoVentaId,
            Codigo = "01",
            Descripcion = revierteStock ? "Anulacion de la operacion" : "Descuento global",
            RevierteStock = revierteStock
        };
        context.MotivoNota.Add(motivo);
        await context.SaveChangesAsync();
        return motivo.Id;
    }

    private static async Task<int> SeedTipoIgvAsync(Infrastructure.Data.SpaContext context, string codigo, bool aplicaPorcentajeImpuesto)
    {
        var tipoIgv = new TipoIgv
        {
            Codigo = codigo,
            Descripcion = aplicaPorcentajeImpuesto ? "Gravado - Operacion Onerosa" : "Exonerado - Operacion Onerosa",
            AplicaPorcentajeImpuesto = aplicaPorcentajeImpuesto
        };
        context.TipoIgv.Add(tipoIgv);
        await context.SaveChangesAsync();
        return tipoIgv.Id;
    }

    private static async Task<int> SeedMetodoPagoAsync(Infrastructure.Data.SpaContext context)
    {
        var metodo = new Metodopago { Nombre = "Efectivo", Descripcion = "Efectivo" };
        context.Metodopago.Add(metodo);
        await context.SaveChangesAsync();
        return metodo.Id;
    }

    private static async Task SeedTipoDocumentoRucAsync(Infrastructure.Data.SpaContext context)
    {
        // Id 5 = RUC en el seed real (tipodocumento.json) -- CrearComprobante lo usa como default
        // al crear un Cliente nuevo a partir de un NumeroDocumento de 11 digitos.
        if (!await context.TipoDocumento.AnyAsync(x => x.Id == 5))
            context.TipoDocumento.Add(new TipoDocumento { Id = 5, Nombre = "RUC" });
        await context.SaveChangesAsync();
    }

    private static async Task<Sucursal> SeedSucursalAsync(Infrastructure.Data.SpaContext context, string nombre, string? serieFactura = null)
    {
        var pais = new Pais { Codigo = "PE", Nombre = "Peru", Idioma = "es", MonedaCodigo = "PEN", TimeZone = "America/Lima", EsquemaFiscal = "SUNAT" };
        context.Pais.Add(pais);
        await context.SaveChangesAsync();

        var moneda = new Moneda { Codigo = "PEN", Simbolo = "S/", Locale = "es-PE", PaisId = pais.Id };
        context.Moneda.Add(moneda);

        var rubro = new Rubro { Nombre = "General" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var sucursal = new Sucursal { Nombre = nombre, MonedaId = moneda.Id, PaisId = pais.Id, RubroId = rubro.Id, SerieFactura = serieFactura };
        context.Sucursal.Add(sucursal);
        await context.SaveChangesAsync();
        return sucursal;
    }

    [Fact]
    public async Task CrearComprobante_SinSeriecorrelativoPrevio_LoCreaYArrancaEnUno()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2, // Boleta
            Total = 20m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        // Precondicion: no existe ninguna fila de Seriecorrelativo todavia.
        Assert.False(await context.Seriecorrelativo.AnyAsync());

        var (estado, resp, message) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var correlativo = await context.Seriecorrelativo.SingleAsync(x => x.Serie == "B001" && x.TipoDocumentoVentaId == 2);
        Assert.Equal(1, correlativo.Correlativo);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(1, cabecera.Correlativo);
        Assert.Equal("B001", cabecera.Serie);

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(8, producto.Stock); // 10 - 2
    }

    [Fact]
    public async Task CrearComprobante_SucursalConSeriePropia_UsaLaSerieDeLaSucursalNoLaDelTenant()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        await SeedTipoDocumentoRucAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var sucursal = await SeedSucursalAsync(context, "Sede Miraflores", serieFactura: "F002");

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 1, // Factura
            NumeroDocumento = "20123456789",
            RazonSocial = "Cliente Test SAC",
            SucursalId = sucursal.Id,
            Total = 20m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        var (estado, _, _) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        // IgnoreQueryFilters: ComprobanteCabecera tiene un filtro que oculta ventas de otras
        // sucursales al usuario ambiente (FakeTenantResolver no tiene SucursalId propio) -- aqui
        // se prueba la resolucion de Serie, no ese filtro de visibilidad.
        var cabecera = await context.ComprobanteCabecera.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("F002", cabecera.Serie); // no el default "F001" del tenant

        var correlativo = await context.Seriecorrelativo.IgnoreQueryFilters().SingleAsync(x => x.Serie == "F002" && x.TipoDocumentoVentaId == 1);
        Assert.Equal(1, correlativo.Correlativo);
    }

    [Fact]
    public async Task CrearComprobante_SucursalSinSeriePropia_CaeALaSerieDelTenant()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        await SeedTipoDocumentoRucAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        // Sucursal sin SerieFactura configurada -- comportamiento de siempre, sin cambios.
        var sucursal = await SeedSucursalAsync(context, "Sede sin serie propia");

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 1, // Factura
            NumeroDocumento = "20123456789",
            RazonSocial = "Cliente Test SAC",
            SucursalId = sucursal.Id,
            Total = 10m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 1, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 10m }
            }
        };

        var (estado, _, _) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("F001", cabecera.Serie);
    }

    [Fact]
    public async Task CrearComprobante_Cotizacion_NoDescuentaStockNiExigeDisponibilidad()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 1);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 6, // Cotizacion
            Total = 100m,
            FechaVigencia = DateTime.UtcNow.AddDays(7),
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                // Pide mas cantidad de la que hay en stock (5 > 1): una cotizacion no valida disponibilidad.
                new() { ProductoId = productoId, Cantidad = 5, ValorUnitario = 20m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 100m }
            }
        };

        var (estado, _, message) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(1, producto.Stock); // sin cambios

        Assert.False(await context.InventoryMovement.AnyAsync());

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal("COT01", cabecera.Serie);
        Assert.NotNull(cabecera.FechaVigencia);
    }

    [Fact]
    public async Task ObtenerCotizacionParaConvertir_Vencida_FallaValidacion()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var (_, cotizacionCreada, _) = await repo.CrearComprobante(new ComprobantePayload
        {
            TipoDocumentoVentaId = 6,
            Total = 20m,
            FechaVigencia = DateTime.UtcNow.AddDays(-1), // ya vencida
            DetalleComprobante = new List<ComprobanteDetallePayload> { new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m } },
            DetallePago = new List<PagoPayload> { new() { MetodoPagoId = metodoPagoId, Monto = 20m } }
        });

        var cotizacionId = (await context.ComprobanteCabecera.SingleAsync()).Id;

        var (estado, _, mensaje) = await repo.ObtenerCotizacionParaConvertir(cotizacionId);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("vencida", mensaje);
    }

    [Fact]
    public async Task ObtenerCotizacionParaConvertir_YaConvertida_FallaValidacion()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        await repo.CrearComprobante(new ComprobantePayload
        {
            TipoDocumentoVentaId = 6,
            Total = 20m,
            DetalleComprobante = new List<ComprobanteDetallePayload> { new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m } },
            DetallePago = new List<PagoPayload> { new() { MetodoPagoId = metodoPagoId, Monto = 20m } }
        });

        var cotizacionId = (await context.ComprobanteCabecera.SingleAsync()).Id;

        // Factura resultante de "convertir" la cotizacion: referencia CotizacionOrigenId.
        await repo.CrearComprobante(new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            CotizacionOrigenId = cotizacionId,
            DetalleComprobante = new List<ComprobanteDetallePayload> { new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m } },
            DetallePago = new List<PagoPayload> { new() { MetodoPagoId = metodoPagoId, Monto = 20m } }
        });

        var (estado, _, mensaje) = await repo.ObtenerCotizacionParaConvertir(cotizacionId);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("convertida", mensaje);
    }

    [Fact]
    public async Task CrearComprobante_DosVentasSeguidas_IncrementaElMismoCorrelativo()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        ComprobantePayload BuildPayload() => new()
        {
            TipoDocumentoVentaId = 2,
            Total = 10m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 1, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 10m }
            }
        };

        var (estado1, _, _) = await repo.CrearComprobante(BuildPayload());
        var (estado2, _, _) = await repo.CrearComprobante(BuildPayload());

        Assert.Equal(ServiceStatus.Ok, estado1);
        Assert.Equal(ServiceStatus.Ok, estado2);

        var correlativo = await context.Seriecorrelativo.SingleAsync(x => x.Serie == "B001" && x.TipoDocumentoVentaId == 2);
        Assert.Equal(2, correlativo.Correlativo);
    }

    [Fact]
    public async Task AnularVenta_RevierteStockYRegistraMovimientoDevolucionVenta()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 5); // ya vendido: stock post-venta

        var cabecera = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 20m,
            ValorSubtotal = 20m,
            ValorIgv = 0m,
            EstadoComprobante = EstatusComprobante.Creado,
        };
        context.ComprobanteCabecera.Add(cabecera);
        await context.SaveChangesAsync();

        context.ComprobanteDetalle.Add(new ComprobanteDetalle
        {
            ComprobanteCabeceraId = cabecera.Id,
            ProductoId = productoId,
            Cantidad = 2,
            ValorUnitario = 10m,
            ValorUnitarioTotal = 20m,
            ValorIgv = 0m,
        });
        await context.SaveChangesAsync();

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var (estado, _, _) = await repo.AnularVenta(cabecera.Id, "Cliente se arrepintio");

        Assert.Equal(ServiceStatus.Ok, estado);

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(7, producto.Stock); // 5 + 2 repuestos

        var cabeceraActualizada = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(EstatusComprobante.Anulado, cabeceraActualizada.EstadoComprobante);

        var movimiento = await context.InventoryMovement.SingleAsync();
        Assert.Equal((int)TipoMovimientoInventario.DevolucionVenta, movimiento.TipoMovimiento);
        Assert.Equal(2, movimiento.Cantidad);
        Assert.Equal(5, movimiento.StockAnterior);
        Assert.Equal(7, movimiento.StockPosterior);
        Assert.Equal("VentaAnulada", movimiento.ReferenciaTipo);
        Assert.Equal(cabecera.Id, movimiento.ReferenciaId);
    }

    [Fact]
    public async Task AnularVenta_SiYaEstaAnulada_NoDuplicaLaDevolucionDeStock()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 5);

        var cabecera = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 10m,
            ValorSubtotal = 10m,
            ValorIgv = 0m,
            EstadoComprobante = EstatusComprobante.Anulado,
        };
        context.ComprobanteCabecera.Add(cabecera);
        await context.SaveChangesAsync();

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var (estado, _, message) = await repo.AnularVenta(cabecera.Id, "Motivo");

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.False(await context.InventoryMovement.AnyAsync());

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(5, producto.Stock); // sin cambios
    }

    [Fact]
    public async Task CrearComprobante_SinTipoIgvEnNingunaLinea_CalculaIgvGlobalComoAntes()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2, // Boleta
            Total = 118m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 1, ValorUnitario = 118m } // sin TipoIgvId
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 118m }
            }
        };

        var (estado, _, _) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(100m, cabecera.ValorSubtotal); // 118 / 1.18
        Assert.Equal(18m, cabecera.ValorIgv);
        Assert.Equal(118m, cabecera.ValorTotal);
    }

    [Fact]
    public async Task CrearComprobante_LineaGravadaYLineaExonerada_CalculaIgvSoloSobreLaGravada()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoGravadoId = await SeedProductoAsync(context, stock: 10);
        var productoExoneradoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);
        var tipoIgvGravadoId = await SeedTipoIgvAsync(context, "10", aplicaPorcentajeImpuesto: true);
        var tipoIgvExoneradoId = await SeedTipoIgvAsync(context, "20", aplicaPorcentajeImpuesto: false);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2, // Boleta
            Total = 168m, // 118 (gravado) + 50 (exonerado)
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoGravadoId, Cantidad = 1, ValorUnitario = 118m, TipoIgvId = tipoIgvGravadoId },
                new() { ProductoId = productoExoneradoId, Cantidad = 1, ValorUnitario = 50m, TipoIgvId = tipoIgvExoneradoId }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 168m }
            }
        };

        var (estado, _, _) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(150m, cabecera.ValorSubtotal); // 100 (base gravada) + 50 (exonerada integra)
        Assert.Equal(18m, cabecera.ValorIgv); // solo de la linea gravada
        Assert.Equal(168m, cabecera.ValorTotal);

        var detalleExonerado = await context.ComprobanteDetalle.SingleAsync(d => d.ProductoId == productoExoneradoId);
        Assert.Equal(0m, detalleExonerado.ValorIgv);
        Assert.Equal(50m, detalleExonerado.ValorUnitarioTotal);
    }

    [Fact]
    public async Task CrearNotaCreditoDebito_MotivoRevierteStock_RestauraStockYCreaCabecera()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 8); // stock post-venta (10 - 2)
        var motivoId = await SeedMotivoNotaAsync(context, tipoDocumentoVentaId: 4, revierteStock: true);

        var afectado = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2, // Boleta
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 20m,
            ValorSubtotal = 20m,
            ValorIgv = 0m,
            EstadoComprobante = EstatusComprobante.Creado,
        };
        context.ComprobanteCabecera.Add(afectado);
        await context.SaveChangesAsync();

        context.ComprobanteDetalle.Add(new ComprobanteDetalle
        {
            ComprobanteCabeceraId = afectado.Id,
            ProductoId = productoId,
            Cantidad = 2,
            ValorUnitario = 10m,
            ValorUnitarioTotal = 20m,
            ValorIgv = 0m,
        });
        await context.SaveChangesAsync();

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new NotaPayload
        {
            ComprobanteAfectadoId = afectado.Id,
            MotivoNotaId = motivoId,
            TipoDocumentoVentaId = 4 // NotaCredito
        };

        var (estado, _, _) = await repo.CrearNotaCreditoDebito(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var producto = await context.Producto.SingleAsync();
        Assert.Equal(10, producto.Stock); // 8 + 2 repuestos

        var nota = await context.ComprobanteCabecera.SingleAsync(c => c.Id != afectado.Id);
        Assert.Equal(4, nota.TipoDocumentoVentaId);
        Assert.Equal(afectado.Id, nota.ComprobanteAfectadoId);
        Assert.Equal(motivoId, nota.MotivoNotaId);
        Assert.Equal("FC01", nota.Serie);
        Assert.Equal(1, nota.Correlativo);

        var detalleNota = await context.ComprobanteDetalle.SingleAsync(d => d.ComprobanteCabeceraId == nota.Id);
        Assert.Equal(productoId, detalleNota.ProductoId);
        Assert.Equal(2, detalleNota.Cantidad);
    }

    [Fact]
    public async Task CrearNotaCreditoDebito_SinComprobanteAfectado_FallaValidacion()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var motivoId = await SeedMotivoNotaAsync(context, tipoDocumentoVentaId: 4, revierteStock: true);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new NotaPayload
        {
            ComprobanteAfectadoId = 9999,
            MotivoNotaId = motivoId,
            TipoDocumentoVentaId = 4
        };

        var (estado, _, _) = await repo.CrearNotaCreditoDebito(payload);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.False(await context.ComprobanteCabecera.AnyAsync());
    }

    [Fact]
    public async Task ListarComprobantes_ExcluyeVentasAnuladas()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);

        context.ComprobanteCabecera.Add(new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = 10m,
            EstadoComprobante = EstatusComprobante.Creado,
        });
        context.ComprobanteCabecera.Add(new ComprobanteCabecera
        {
            TipoDocumentoVentaId = 2,
            Serie = "B001",
            Correlativo = 2,
            ValorTotal = 99m,
            EstadoComprobante = EstatusComprobante.Anulado,
        });
        await context.SaveChangesAsync();

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var (estado, resultado, _) = await repo.ListarComprobantes(new ComprobanteQueryParams { Page = 1, Amount = 20 });

        Assert.Equal(ServiceStatus.Ok, estado);
        var lista = Assert.IsType<DataCollection<Domain.DTO.ComprobanteCabeceraDTO>>(resultado);
        Assert.Single(lista.Items);
        Assert.Equal(10m, lista.Items.Single().ValorTotal);
    }

    [Fact]
    public async Task ObtenerLibroVentas_FacturaConLineaGravadaYExonerada_SeparaLosTotalesPorTipoIgv()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        await SeedTipoDocumentoRucAsync(context);
        var productoGravadoId = await SeedProductoAsync(context, stock: 10);
        var productoExoneradoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);
        var tipoIgvGravadoId = await SeedTipoIgvAsync(context, "10", aplicaPorcentajeImpuesto: true);
        var tipoIgvExoneradoId = await SeedTipoIgvAsync(context, "20", aplicaPorcentajeImpuesto: false);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 1, // Factura
            NumeroDocumento = "20123456789",
            RazonSocial = "Cliente Test SAC",
            Total = 168m, // 118 (gravado con igv) + 50 (exonerado)
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoGravadoId, Cantidad = 1, ValorUnitario = 118m, TipoIgvId = tipoIgvGravadoId },
                new() { ProductoId = productoExoneradoId, Cantidad = 1, ValorUnitario = 50m, TipoIgvId = tipoIgvExoneradoId }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 168m }
            }
        };

        var (estadoCrear, _, _) = await repo.CrearComprobante(payload);
        Assert.Equal(ServiceStatus.Ok, estadoCrear);

        var (estado, libro, _) = await repo.ObtenerLibroVentas(new ContabilidadQueryParams());

        Assert.Equal(ServiceStatus.Ok, estado);
        var fila = Assert.Single(libro!);
        Assert.Equal("01", fila.TipoComprobante); // Factura -> Tabla 10
        Assert.Equal("6", fila.TipoDocCliente); // RUC de 11 digitos -> Tabla 2
        Assert.Equal(100.00m, fila.BaseImponibleGravada);
        Assert.Equal(50.00m, fila.OpExonerada);
        Assert.Equal(0m, fila.OpInafecta);
        Assert.Equal(18.00m, fila.Igv);
        Assert.Equal(168m, fila.ImporteTotal);
        Assert.Equal("1", fila.Estado); // Activo -> Tabla 17
    }

    [Fact]
    public async Task ObtenerLibroVentas_NotaDeVentaYCotizacion_NoAparecenEnElLibro()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 3, // Nota de Venta -- no es comprobante fiscal
            Total = 20m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 1, ValorUnitario = 20m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        var (estadoCrear, _, _) = await repo.CrearComprobante(payload);
        Assert.Equal(ServiceStatus.Ok, estadoCrear);

        var (estado, libro, _) = await repo.ObtenerLibroVentas(new ContabilidadQueryParams());

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Empty(libro!);
    }
}
