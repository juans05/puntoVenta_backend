using System.IO.Compression;
using System.Text;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class SunatPropuestaTests
{
    // Encabezados como los del txt de la propuesta RVIE: la primera "razon social" es la del emisor
    // y la ultima la del cliente; las columnas se buscan por nombre, con o sin tildes.
    private const string Txt =
        "RUC|Apellidos y Nombres o Razón social|Periodo|CAR SUNAT|Fecha de emisión|Fecha Vcto/Pago|Tipo CP/Doc.|Serie del CDP|Nro CP o Doc. Nro Inicial (Rango)|Nro Final (Rango)|Tipo Doc Identidad|Nro Doc Identidad|Apellidos Nombres/ Razón Social|Valor Facturado Exportación|BI Gravada|Dscto BI|IGV / IPM|Dscto IGV / IPM|Mto Exonerado|Mto Inafecto|ISC|BI Grav IVAP|IVAP|ICBPER|Otros Tributos|Total CP|Moneda\r\n" +
        "10430936315|MI EMPRESA|202605|X|05/05/2026||01|F001|25||6|20100066603|CLIENTE SAC|0.00|100.00|0.00|18.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|118.00|PEN\r\n" +
        "10430936315|MI EMPRESA|202605|X|06/05/2026||03|B001|7||1|12345678|JUAN PEREZ|0.00|50.00|0.00|9.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|59.00|PEN\r\n";

    [Fact]
    public void LeePropuestaDesdeZip_UbicaColumnasPorNombre()
    {
        var zip = new MemoryStream();
        using (var archivo = new ZipArchive(zip, ZipArchiveMode.Create, true))
        using (var escritor = new StreamWriter(archivo.CreateEntry("20100176450-CPF-202605-01.txt").Open(), Encoding.UTF8))
            escritor.Write(Txt);

        var filas = SunatRepository.LeerPropuesta(zip.ToArray())!;

        Assert.Equal(2, filas.Count);
        Assert.Equal(("F001", "25", "01", "05/05/2026"), (filas[0].Serie, filas[0].Numero, filas[0].Tipo, filas[0].Fecha));
        Assert.Equal(("20100066603", "CLIENTE SAC"), (filas[0].DocCliente, filas[0].Cliente));
        Assert.Equal((100m, 18m, 118m, "PEN"), (filas[0].BaseGravada, filas[0].Igv, filas[0].Total, filas[0].Moneda));
        Assert.Equal(59m, filas[1].Total);
    }

    // Propuesta RCE (compras): la ultima "razon social" y el "Nro Doc Identidad" son del proveedor,
    // la base viene como "BI Gravado DG" y trae fecha de vencimiento.
    [Fact]
    public void LeePropuestaDeCompras_TomaProveedorBaseYVencimiento()
    {
        const string txtRce =
            "RUC|Apellidos y Nombres o Razón social|Periodo|CAR SUNAT|Fecha de emisión|Fecha Vcto/Pago|Tipo CP/Doc.|Serie del CDP|Año|Nro CP o Doc. Nro Inicial (Rango)|Nro Final (Rango)|Tipo Doc Identidad|Nro Doc Identidad|Apellidos Nombres/ Razón Social|BI Gravado DG|IGV / IPM DG|BI Gravado DGNG|IGV / IPM DGNG|BI Gravado DNG|IGV / IPM DNG|Valor Adq. NG|ISC|ICBPER|Otros Trib/ Cargos|Total CP|Moneda\n" +
            "10430936315|MI EMPRESA|202605|X|05/05/2026|04/06/2026|01|F002||0000341||6|20100066603|PROVEEDOR SAC|200.00|36.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|236.00|PEN\n";

        var f = SunatRepository.LeerPropuesta(Encoding.UTF8.GetBytes(txtRce))!.Single();

        Assert.Equal(("F002", "0000341", "01"), (f.Serie, f.Numero, f.Tipo));
        Assert.Equal(("20100066603", "PROVEEDOR SAC"), (f.DocCliente, f.Cliente));
        Assert.Equal((200m, 36m, 236m), (f.BaseGravada, f.Igv, f.Total));
        Assert.Equal(("05/05/2026", "04/06/2026"), (f.Fecha, f.FechaVencimiento));
    }

    [Fact]
    public async Task RegistrarVentas_RegistraLaVentaYaAceptadaConSuAsiento_YNoDuplica()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 1, Nombre = "Factura" });
        context.TipoDocumento.Add(new TipoDocumento { Id = 5, Nombre = "RUC" });
        await context.SaveChangesAsync();
        var repo = new SunatRepository(context, null!, null!, new AsientoContableRepository(context));
        var fila = new SunatRepository.ComprobanteSunat("12/09/2026", "01", "E001", "733", "20548491763", "ARMONHY SAC", 677.97m, 122.03m, 800m, "PEN");
        var boletaUsd = fila with { Numero = "734", Moneda = "USD" };

        var (importados, omitidos) = await repo.RegistrarVentas(new() { fila, boletaUsd });
        var (otraVez, omitidosOtraVez) = await repo.RegistrarVentas(new() { fila });

        Assert.True(importados == 1, string.Join("; ", omitidos));
        Assert.Contains(omitidos, o => o.StartsWith("E001-734"));
        Assert.Equal(0, otraVez);
        Assert.Contains(omitidosOtraVez, o => o.Contains("ya está"));

        var venta = await context.ComprobanteCabecera.Include(c => c.Cliente).SingleAsync(c => c.Serie == "E001");
        Assert.Equal((733, 800m, 677.97m, 'E', true), (venta.Correlativo, venta.ValorTotal, venta.ValorSubtotal, venta.EnviadoSunat, venta.EsCredito));
        Assert.Equal("ARMONHY SAC", venta.Cliente!.Nombre);

        var asiento = await context.AsientoContable.Include(a => a.Detalle).ThenInclude(d => d.CuentaContable)
            .SingleAsync(a => a.OrigenTipo == OrigenAsientoContable.Venta && a.OrigenId == venta.Id);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "12" && d.Debe == 800m);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "70" && d.Haber == 677.97m);
        Assert.Contains(asiento.Detalle, d => d.CuentaContable!.Codigo == "40111" && d.Haber == 122.03m);
    }

    [Fact]
    public void SinColumnasReconocibles_DevuelveNull()
        => Assert.Null(SunatRepository.LeerPropuesta(Encoding.UTF8.GetBytes("a|b|c\r\n1|2|3")));
}
