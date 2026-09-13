using Domain.Utils;
using Xunit;

namespace Infrastructure.Tests;

public class NumerosALetrasTests
{
    [Theory]
    [InlineData(999.99, "SON NOVECIENTOS NOVENTA Y NUEVE CON NOVENTA Y NUEVE/100 SOLES")]
    [InlineData(1000, "SON MIL CON 00/100 SOLES")]
    [InlineData(1500.51, "SON MIL QUINIENTOS CON CINCUENTA Y UN/100 SOLES")]
    [InlineData(21000, "SON VEINTE Y UN MIL CON 00/100 SOLES")]
    [InlineData(100000, "SON CIENTO MIL CON 00/100 SOLES")]
    [InlineData(1000000, "SON UN MILLON CON 00/100 SOLES")]
    public void ConvertirNumeroALetras_MontosDeMilesYMillones_NoLanzaYFormateaCorrecto(decimal monto, string esperado)
    {
        var resultado = DecimalExtensions.ConvertirNumeroALetras(monto);

        Assert.Equal(esperado, resultado);
    }
}
