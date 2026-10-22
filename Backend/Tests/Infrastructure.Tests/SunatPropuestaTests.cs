using System.IO.Compression;
using System.Text;
using Infrastructure.Repositories;
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

    [Fact]
    public void SinColumnasReconocibles_DevuelveNull()
        => Assert.Null(SunatRepository.LeerPropuesta(Encoding.UTF8.GetBytes("a|b|c\r\n1|2|3")));
}
