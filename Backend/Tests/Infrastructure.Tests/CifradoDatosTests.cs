using Infrastructure.Data;
using Xunit;

namespace Infrastructure.Tests;

public class CifradoDatosTests
{
    [Fact]
    public void CifraYDescifra_LeeTextoPlanoAntiguo_YNoLeeConOtraClave()
    {
        CifradoDatos.Configurar("clave-de-prueba-1");
        var cifrado = CifradoDatos.Cifrar("token-secreto");

        Assert.StartsWith("enc:v1:", cifrado);
        Assert.DoesNotContain("token-secreto", cifrado);
        Assert.Equal("token-secreto", CifradoDatos.Descifrar(cifrado));
        Assert.Equal("valor-antiguo", CifradoDatos.Descifrar("valor-antiguo"));

        CifradoDatos.Configurar("otra-clave");
        Assert.Null(CifradoDatos.Descifrar(cifrado));

        CifradoDatos.Configurar("YOUR_ENCRYPTION_SECRET");
        Assert.Equal("sin-cifrar", CifradoDatos.Cifrar("sin-cifrar"));
    }
}
