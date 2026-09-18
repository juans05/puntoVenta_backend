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

    [Fact]
    public async Task ImportarXmlCompra_ProveedorNuevo_LoCreaYDevuelveLasLineas()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

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

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

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

        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

        var (estado, preview, mensaje) = await repo.ImportarXmlCompra(ComoStream("esto no es xml"));

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Null(preview);
    }

    [Fact]
    public async Task CrearCompra_SinTipoIgvNiDescuento_TotalIgualQueAntes()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

        var (estado, dto, _) = await repo.CrearCompra(new CreateCompraPayload
        {
            Detalle = new List<CompraDetallePayload> { new() { ProductoId = productoId, Cantidad = 1, CostoUnitario = 118m } }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(118m, dto!.Total);
        Assert.Equal(100.00m, dto.ValorGravada);
        Assert.Equal(18.00m, dto.ValorIgv);
    }

    [Fact]
    public async Task CrearCompra_ConDescuentoPorcentualYOtrosCargos_CalculaTotalYGravadaIgv()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var productoId = await SeedProductoAsync(context);
        var tipoIgvGravadoId = await SeedTipoIgvAsync(context, "10", aplicaPorcentajeImpuesto: true);
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

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
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

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
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

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
        var repo = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

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
