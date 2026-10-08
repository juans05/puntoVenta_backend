using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Xunit;

namespace Infrastructure.Tests;

public class CompraRepositoryTests
{
    // XML UBL 2.1 minimo (formato estandar de factura electronica SUNAT) con un proveedor y
    // una linea de detalle -- es el mismo formato que emite cualquier facturador SUNAT real.
    private static string XmlFacturaValida(string ruc, string razonSocial) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Invoice xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2""
         xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
         xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"">
  <cbc:ID>F001-123</cbc:ID>
  <cbc:IssueDate>2026-09-10</cbc:IssueDate>
  <cac:AccountingSupplierParty>
    <cac:Party>
      <cac:PartyIdentification><cbc:ID>{ruc}</cbc:ID></cac:PartyIdentification>
      <cac:PartyLegalEntity><cbc:RegistrationName>{razonSocial}</cbc:RegistrationName></cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingSupplierParty>
  <cac:LegalMonetaryTotal><cbc:PayableAmount>118.00</cbc:PayableAmount></cac:LegalMonetaryTotal>
  <cac:InvoiceLine>
    <cbc:InvoicedQuantity>2</cbc:InvoicedQuantity>
    <cac:Item><cbc:Description>Producto de prueba</cbc:Description></cac:Item>
    <cac:Price><cbc:PriceAmount>50.00</cbc:PriceAmount></cac:Price>
  </cac:InvoiceLine>
</Invoice>";

    private static System.IO.Stream ComoStream(string xml) => new System.IO.MemoryStream(Encoding.UTF8.GetBytes(xml));

    private static async Task<int> SeedProductoAsync(Infrastructure.Data.SpaContext context, decimal costo = 10m)
    {
        var producto = new Producto { Nombre = "Producto compra test", Precio = costo, Stock = 0, RestriccionEdad = 0 };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();
        return producto.Id;
    }

    private static async Task<int> SeedTipoIgvAsync(Infrastructure.Data.SpaContext context, string codigo, bool aplicaPorcentajeImpuesto)
    {
        var tipoIgv = new TipoIgv { Codigo = codigo, Descripcion = codigo, AplicaPorcentajeImpuesto = aplicaPorcentajeImpuesto };
        context.TipoIgv.Add(tipoIgv);
        await context.SaveChangesAsync();
        return tipoIgv.Id;
    }

    // TipoDocumentoVentaId 4=NotaCredito/5=NotaDebito, mismo catalogo que usa Ventas (ver
    // ComprobanteRepositoryTests.SeedMotivoNotaAsync) -- NotaCompra lo reusa tal cual.
    private static async Task<int> SeedMotivoNotaCompraAsync(Infrastructure.Data.SpaContext context, bool esCredito, bool revierteStock)
    {
        var motivo = new MotivoNota
        {
            TipoDocumentoVentaId = esCredito ? 4 : 5,
            Codigo = "01",
            Descripcion = esCredito ? "Devolucion" : "Cargo adicional",
            RevierteStock = revierteStock
        };
        context.MotivoNota.Add(motivo);
        await context.SaveChangesAsync();
        return motivo.Id;
    }

    [Fact]
    public async Task ImportarXmlCompra_ProveedorNuevo_LoCreaYDevuelveLasLineas()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, preview, _) = await repo.ImportarXmlCompra(ComoStream(XmlFacturaValida("20123456789", "Distribuidora Acme SAC")));

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(preview);
        Assert.Equal("20123456789", preview!.ProveedorRuc);
        Assert.Equal("Distribuidora Acme SAC", preview.ProveedorNombre);
        Assert.Equal("F001-123", preview.NumeroDocumento);
        Assert.Equal(118.00m, preview.Total);
        Assert.Single(preview.Lineas);
        Assert.Equal("Producto de prueba", preview.Lineas[0].Descripcion);
        Assert.Equal(2, preview.Lineas[0].Cantidad);
        Assert.Equal(50.00m, preview.Lineas[0].PrecioUnitario);

        var proveedorCreado = await context.Proveedor.SingleAsync();
        Assert.Equal("20123456789", proveedorCreado.Ruc);
        Assert.Equal(preview.ProveedorId, proveedorCreado.Id);
    }

    [Fact]
    public async Task ImportarXmlCompra_ProveedorYaExiste_LoEmparejaSinDuplicar()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var proveedorExistente = new Proveedor { Nombre = "Nombre Registrado Distinto", Ruc = "20999999999" };
        context.Proveedor.Add(proveedorExistente);
        await context.SaveChangesAsync();

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, preview, _) = await repo.ImportarXmlCompra(ComoStream(XmlFacturaValida("20999999999", "Otro Nombre En El Xml")));

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(proveedorExistente.Id, preview!.ProveedorId);
        Assert.Equal(1, await context.Proveedor.CountAsync()); // no duplico
    }

    [Fact]
    public async Task ImportarXmlCompra_XmlInvalido_FallaValidacion()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, preview, mensaje) = await repo.ImportarXmlCompra(ComoStream("esto no es xml"));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Null(preview);
    }

    [Fact]
    public async Task CrearCompra_SegundaCompraADistintoPrecio_PromediaElCosto()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 10, CostoUnitario = 10m } }
        });
        var (_, compra2, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 10, CostoUnitario = 20m } }
        });

        var producto = await context.Producto.AsNoTracking().SingleAsync();
        Assert.Equal(20, producto.Stock);
        Assert.Equal(15m, producto.CostoUnitario); // (10*10 + 10*20) / 20

        await repo.AnularCompra(compra2!.Id);

        var productoTrasAnular = await context.Producto.AsNoTracking().SingleAsync();
        Assert.Equal(10, productoTrasAnular.Stock);
        Assert.Equal(10m, productoTrasAnular.CostoUnitario); // vuelve al costo de la primera compra
    }

    [Fact]
    public async Task CrearCompra_SinTipoIgvNiDescuento_TotalIgualQueAntes()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 118m } }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(118m, dto!.Total);
        Assert.Equal(100.00m, dto.ValorGravada);
        Assert.Equal(18.00m, dto.ValorIgv);
    }

    // Dos lineas con distinta CuentaContableId (o sin elegir, que cae en la cuenta por defecto de
    // Si el proveedor tiene su propia Cuenta por Pagar (configurada al crearlo), la factura postea
    // ahi en vez de "42" -- ver "la configuracion de la cuenta contable va a ser al momento de
    // crear ... un socio de negocio".
    [Fact]
    public async Task CrearCompra_ConProveedorConCuentaPorPagarPropia_PosteaEnEsaCuentaEnVezDe42()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var cuenta4212 = new CuentaContable { Codigo = "42120000", Nombre = "Proveedor especial", Tipo = TipoCuentaContable.Pasivo, Nivel = 5, ClaseCuenta = "42" };
        context.CuentaContable.Add(cuenta4212);
        var proveedor = new Proveedor { Nombre = "Proveedor especial", CuentaPorPagar = cuenta4212 };
        context.Proveedor.Add(proveedor);
        await context.SaveChangesAsync();

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, mensaje) = await repo.CrearCompra(new CreateCompraPayload
        {
            ProveedorId = proveedor.Id,
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 118m } }
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        var asiento = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == "Factura" && a.OrigenId == dto!.Id);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "42120000" && d.Haber == dto!.Total);
        Assert.DoesNotContain(asiento.Detalle, d => d.CuentaContable!.Codigo == "42");
    }

    // compra directa "20") deben separarse en el asiento, sumando exacto a ValorGravada.
    [Fact]
    public async Task CrearCompra_ConCuentaContablePorLinea_AgrupaElAsientoPorCuentaYCuadraConGravada()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId1 = await SeedProductoAsync(context);
        var productoId2 = await SeedProductoAsync(context);
        var cuenta63Id = await context.CuentaContable.AsNoTracking().Where(c => c.Codigo == "63").Select(c => c.Id).SingleAsync();
        var tipoIgvExoneradoId = await SeedTipoIgvAsync(context, "20", aplicaPorcentajeImpuesto: false);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, mensaje) = await repo.CrearCompra(new CreateCompraPayload
        {
            TipoIgvId = tipoIgvExoneradoId,
            Detalle = new List<CompraDetallePayload>
            {
                new() { ProductoId = productoId1, Cantidad = 1, CostoUnitario = 100m }, // sin cuenta -> "20"
                new() { ProductoId = productoId2, Cantidad = 1, CostoUnitario = 50m, CuentaContableId = cuenta63Id },
            }
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(150m, dto!.ValorGravada);

        var asiento = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == "Factura" && a.OrigenId == dto.Id);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "60" && d.Debe == 100m);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "63" && d.Debe == 50m);

        // Ambas lineas son bienes: las dos entran al almacen (20/61) por el total, aunque una se facturo a 63.
        var entrada = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == OrigenAsientoContable.EntradaCompra && a.OrigenId == dto.Id);
        Assert.Contains(entrada.Detalle, d => d.CuentaContable!.Codigo == "20" && d.Debe == 150m);
        Assert.Contains(entrada.Detalle, d => d.CuentaContable!.Codigo == "61" && d.Haber == 150m);
    }

    // Linea sin cuenta elegida: usa la cuenta del producto (servicio -> gasto Debe; bien -> inventario de Compra).
    [Fact]
    public async Task CrearCompra_SinCuentaEnLinea_UsaLaCuentaDelProducto()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var cuentaGasto = new CuentaContable { Codigo = "63110000", Nombre = "Transporte", Tipo = TipoCuentaContable.Gasto, Nivel = 5, ClaseCuenta = "63" };
        var cuentaInv = new CuentaContable { Codigo = "20111000", Nombre = "Mercaderias propias", Tipo = TipoCuentaContable.Activo, Nivel = 5, ClaseCuenta = "20" };
        context.CuentaContable.AddRange(cuentaGasto, cuentaInv);
        await context.SaveChangesAsync();
        var servicio = new Producto { Nombre = "Flete", Precio = 1, RestriccionEdad = 0, EsServicio = true, CuentaCostoId = cuentaGasto.Id };
        var bien = new Producto { Nombre = "Caja", Precio = 1, Stock = 0, RestriccionEdad = 0,
            CuentasInventarioMovimiento = $"{{\"{(int)Domain.Enumerations.TipoMovimientoInventario.Compra}\":{cuentaInv.Id}}}" };
        context.Producto.AddRange(servicio, bien);
        await context.SaveChangesAsync();
        var tipoIgvExoneradoId = await SeedTipoIgvAsync(context, "20", aplicaPorcentajeImpuesto: false);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, mensaje) = await repo.CrearCompra(new CreateCompraPayload
        {
            TipoIgvId = tipoIgvExoneradoId,
            Detalle = new List<CompraDetallePayload>
            {
                new() { ProductoId = servicio.Id, Cantidad = 1, CostoUnitario = 30m },
                new() { ProductoId = bien.Id, Cantidad = 2, CostoUnitario = 10m },
            }
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        var asiento = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == "Factura" && a.OrigenId == dto!.Id);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "63110000" && d.Debe == 30m);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "60" && d.Debe == 20m);

        var entrada = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == OrigenAsientoContable.EntradaCompra && a.OrigenId == dto!.Id);
        Assert.Contains(entrada.Detalle, d => d.CuentaContable!.Codigo == "20111000" && d.Debe == 20m); // solo el bien, no el servicio
        Assert.Contains(entrada.Detalle, d => d.CuentaContable!.Codigo == "61" && d.Haber == 20m);
    }

    // ModoCentroCosto=OBLIGATORIO en la cuenta (ver "configuracion plan de cuentas ERPdocx.docx"):
    // cada linea que postee a esa cuenta debe traer su propio CentroCostoId. Dos tests separados
    // (no uno con dos llamadas en el mismo context): el primer CrearCompra hace rollback de
    // transaccion y el Compra creado queda trackeado en memoria, chocando con el Id del segundo.
    [Fact]
    public async Task CrearCompra_ConCuentaQueExigeCentroCosto_SinCentroCosto_FailedValidation()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var cuenta63 = await context.CuentaContable.AsTracking().SingleAsync(c => c.Codigo == "63");
        cuenta63.ModoCentroCosto = ModoCentroCostoCuenta.Obligatorio;
        await context.SaveChangesAsync();

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, _, mensaje) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 10m, CuentaContableId = cuenta63.Id } }
        });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("centro de costo", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearCompra_ConCuentaQueExigeCentroCosto_ConCentroCosto_Ok()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var cuenta63 = await context.CuentaContable.AsTracking().SingleAsync(c => c.Codigo == "63");
        cuenta63.ModoCentroCosto = ModoCentroCostoCuenta.Obligatorio;
        var centroCosto = new CentroCosto { Nombre = "Administración" };
        context.CentroCosto.Add(centroCosto);
        await context.SaveChangesAsync();

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, mensaje) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 10m, CuentaContableId = cuenta63.Id, CentroCostoId = centroCosto.Id } }
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.NotNull(dto);
    }

    [Fact]
    public async Task NotaCredito_RevierteStockYGeneraAsientoInvertidoDeLaFactura()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);
        var (_, compra, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 3, CostoUnitario = 10m } }
        });
        var stockTrasCompra = (await context.Producto.AsNoTracking().SingleAsync()).Stock;
        var motivoId = await SeedMotivoNotaCompraAsync(context, esCredito: true, revierteStock: true);

        var (estado, nota, mensaje) = await repo.CrearNotaCompra(new CrearNotaCompraPayload
        {
            CompraId = compra!.Id, Tipo = TipoNotaCompra.Credito, MotivoNotaId = motivoId
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(compra.Total, nota!.Monto);
        Assert.Equal(stockTrasCompra - 3, (await context.Producto.AsNoTracking().SingleAsync()).Stock);

        var asientoFactura = await context.AsientoContable.AsNoTracking()
            .SingleAsync(a => a.OrigenTipo == "Factura" && a.OrigenId == compra.Id);
        Assert.Equal(EstadoAsientoContable.Emitido, asientoFactura.EstadoAsiento); // la nota NO anula la factura original

        var asientoNota = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == "NotaCompra" && a.OrigenId == nota.Id);
        Assert.Contains(asientoNota.Detalle, d => d.CuentaContable!.Codigo == "42" && d.Debe == compra.Total); // invertido vs la factura
        Assert.Contains(asientoNota.Detalle, d => d.CuentaContable!.Codigo == "60" && d.Haber == compra.ValorGravada);
        Assert.Contains(asientoNota.Detalle, d => d.CuentaContable!.Codigo == "40111" && d.Haber == compra.ValorIgv);

        var asientoDevolucion = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == OrigenAsientoContable.NotaCompraEntrada && a.OrigenId == nota.Id);
        Assert.Contains(asientoDevolucion.Detalle, d => d.CuentaContable!.Codigo == "61" && d.Debe == compra.ValorGravada);
        Assert.Contains(asientoDevolucion.Detalle, d => d.CuentaContable!.Codigo == "20" && d.Haber == compra.ValorGravada);
    }

    [Fact]
    public async Task NotaDebito_NoTocaStockYGeneraAsientoEnElMismoSentidoDeLaFactura()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);
        var (_, compra, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 3, CostoUnitario = 10m } }
        });
        var stockTrasCompra = (await context.Producto.AsNoTracking().SingleAsync()).Stock;
        var motivoId = await SeedMotivoNotaCompraAsync(context, esCredito: false, revierteStock: false);

        var (estado, nota, mensaje) = await repo.CrearNotaCompra(new CrearNotaCompraPayload
        {
            CompraId = compra!.Id, Tipo = TipoNotaCompra.Debito, MotivoNotaId = motivoId
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(stockTrasCompra, (await context.Producto.AsNoTracking().SingleAsync()).Stock); // sin cambios

        var asientoNota = await context.AsientoContable.AsNoTracking().Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == "NotaCompra" && a.OrigenId == nota!.Id);
        Assert.Contains(asientoNota.Detalle, d => d.CuentaContable!.Codigo == "60" && d.Debe == compra.ValorGravada); // mismo sentido que la factura
        Assert.Contains(asientoNota.Detalle, d => d.CuentaContable!.Codigo == "40111" && d.Debe == compra.ValorIgv);
        Assert.Contains(asientoNota.Detalle, d => d.CuentaContable!.Codigo == "42" && d.Haber == compra.Total);
    }

    [Fact]
    public async Task AnularNotaCompra_DevuelveElStockYReversaElAsiento()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);
        var (_, compra, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 3, CostoUnitario = 10m } }
        });
        var motivoId = await SeedMotivoNotaCompraAsync(context, esCredito: true, revierteStock: true);
        var (_, nota, _) = await repo.CrearNotaCompra(new CrearNotaCompraPayload
        {
            CompraId = compra!.Id, Tipo = TipoNotaCompra.Credito, MotivoNotaId = motivoId
        });
        var stockTrasNota = (await context.Producto.AsNoTracking().SingleAsync()).Stock;

        var (estado, anulada, mensaje) = await repo.AnularNotaCompra(nota!.Id);

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal(EstadoNotaCompra.Anulada, anulada!.EstadoNota);
        Assert.Equal(stockTrasNota + 3, (await context.Producto.AsNoTracking().SingleAsync()).Stock);

        var asientos = await context.AsientoContable.AsNoTracking().Where(a => a.OrigenTipo == "NotaCompra" && a.OrigenId == nota.Id).ToListAsync();
        Assert.Equal(2, asientos.Count);
        Assert.Contains(asientos, a => a.EstadoAsiento == EstadoAsientoContable.Anulado);
        Assert.Contains(asientos, a => a.EstadoAsiento == EstadoAsientoContable.Emitido);
    }

    [Fact]
    public async Task CrearCompra_ConDescuentoPorcentualYOtrosCargos_CalculaTotalYGravadaIgv()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var tipoIgvGravadoId = await SeedTipoIgvAsync(context, "10", aplicaPorcentajeImpuesto: true);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            TipoIgvId = tipoIgvGravadoId,
            PorcentajeDescuento = 10m,
            OtrosCargos = 5m,
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 2, CostoUnitario = 59m } }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(111.20m, dto!.Total);
        Assert.Equal(11.80m, dto.MontoDescuento);
        Assert.Equal(94.24m, dto.ValorGravada);
        Assert.Equal(16.96m, dto.ValorIgv);
    }

    [Fact]
    public async Task CrearCompra_TipoIgvExonerado_NoCalculaIgv()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var tipoIgvExoneradoId = await SeedTipoIgvAsync(context, "20", aplicaPorcentajeImpuesto: false);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            TipoIgvId = tipoIgvExoneradoId,
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 50m } }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(50m, dto!.Total);
        Assert.Equal(50m, dto.ValorGravada);
        Assert.Equal(0m, dto.ValorIgv);
    }

    [Fact]
    public async Task CrearCompra_ProveedorNoExiste_LoCreaPorRucYAsociaLaCompra()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estado, dto, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            ProveedorRuc = "20111222333",
            ProveedorNombre = "Nuevo Proveedor SAC",
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 10m } }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal("Nuevo Proveedor SAC", dto!.Proveedor);
        var proveedorCreado = await context.Proveedor.SingleAsync();
        Assert.Equal("20111222333", proveedorCreado.Ruc);
    }

    [Fact]
    public async Task ObtenerLibroCompras_CompraGravada_MapeaCamposDelPle()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var tipoIgvGravadoId = await SeedTipoIgvAsync(context, "10", aplicaPorcentajeImpuesto: true);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, new AsientoContableRepository(context), httpContextAccessor: null);

        var (estadoCrear, _, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            ProveedorRuc = "20111222333",
            ProveedorNombre = "Distribuidora Test SAC",
            Serie = "F001",
            Numero = "000123",
            TipoIgvId = tipoIgvGravadoId,
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 118m } }
        });
        Assert.Equal(ServiceStatus.Ok, estadoCrear);

        var (estado, libro, _) = await repo.ObtenerLibroCompras(new ContabilidadQueryParams());

        Assert.Equal(ServiceStatus.Ok, estado);
        var fila = Assert.Single(libro!);
        Assert.Equal("6", fila.TipoDocProveedor); // RUC de 11 digitos -> Tabla 2
        Assert.Equal("20111222333", fila.NumeroDocProveedor);
        Assert.Equal("F001", fila.Serie);
        Assert.Equal("000123", fila.Numero);
        Assert.Equal(100.00m, fila.BaseImponibleGravada);
        Assert.Equal(0m, fila.ValorAdquisicionesNoGravadas);
        Assert.Equal(18.00m, fila.Igv);
        Assert.Equal(118m, fila.ImporteTotal);
    }
}
