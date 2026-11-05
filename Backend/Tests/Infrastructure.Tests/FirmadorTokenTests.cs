using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Infrastructure.Identidad;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Infrastructure.Tests;

public class FirmadorTokenTests
{
    private const string Secreto = "clave-hmac-de-prueba-suficientemente-larga-para-hs256";

    private static string NuevoPem()
        => new string(PemEncoding.Write("RSA PRIVATE KEY", RSA.Create(2048).ExportRSAPrivateKey()));

    private static string Firmar(FirmadorToken firmador)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("username", "TEST") }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = firmador.Credenciales()
        }));
    }

    private static void Validar(string token, IEnumerable<SecurityKey> claves)
        => new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            IssuerSigningKeys = claves,
            ValidateIssuer = false,
            ValidateAudience = false
        }, out _);

    [Fact]
    public void ConRsa_TokenFirmadoValida_SoloConClavePublicaDelJwks()
    {
        var firmador = new FirmadorToken(Secreto, NuevoPem());
        var token = Firmar(firmador);

        Validar(token, firmador.Jwks().GetSigningKeys());
        Assert.Equal("RS256", new JwtSecurityTokenHandler().ReadJwtToken(token).Header.Alg);
    }

    [Fact]
    public void ConRsa_TokenHmacAnterior_SigueValido()
    {
        var tokenAnterior = Firmar(new FirmadorToken(Secreto, null));
        Validar(tokenAnterior, new FirmadorToken(Secreto, NuevoPem()).ClavesValidacion());
    }

    [Fact]
    public void SinRsa_FirmaHmac_YJwksVacio()
    {
        var firmador = new FirmadorToken(Secreto, null);
        Assert.Equal(SecurityAlgorithms.HmacSha256Signature, firmador.Credenciales().Algorithm);
        Assert.Empty(firmador.Jwks().Keys);
    }

    [Fact]
    public void Jwks_NoExponeParametrosPrivados()
    {
        var clave = Assert.Single(new FirmadorToken(Secreto, NuevoPem()).Jwks().Keys);
        Assert.Null(clave.D);
        Assert.Null(clave.P);
        Assert.Null(clave.Q);
        Assert.Null(clave.DP);
        Assert.Null(clave.DQ);
        Assert.Null(clave.QI);
        Assert.False(string.IsNullOrEmpty(clave.Kid));
    }

    // Formato tipico al pegar la clave en una variable de entorno (Railway/.env): una linea con "\n" escapados, entre comillas.
    [Fact]
    public void PemEscapadoYEntreComillas_SeAcepta()
    {
        var pemEnUnaLinea = "\"" + NuevoPem().Replace("\r\n", "\n").Replace("\n", "\\n") + "\"";
        var firmador = new FirmadorToken(Secreto, pemEnUnaLinea);

        Validar(Firmar(firmador), firmador.Jwks().GetSigningKeys());
    }

    [Fact]
    public void PemDeClavePublica_FallaAlArrancarNombrandoLaVariable()
    {
        var publica = new string(PemEncoding.Write("PUBLIC KEY", RSA.Create(2048).ExportSubjectPublicKeyInfo()));

        var ex = Assert.Throws<InvalidOperationException>(() => new FirmadorToken(Secreto, publica));
        Assert.Contains("RsaPrivateKeyPem", ex.Message);
    }

    [Fact]
    public void PemInvalido_FallaAlArrancarNombrandoLaVariable()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new FirmadorToken(Secreto, "no-es-un-pem"));
        Assert.Contains("RsaPrivateKeyPem", ex.Message);
    }

    [Fact]
    public void ConRsa_TokenFirmadoConOtraClaveRsa_NoValida()
    {
        var token = Firmar(new FirmadorToken(Secreto, NuevoPem()));
        var otro = new FirmadorToken(Secreto, NuevoPem());

        Assert.ThrowsAny<SecurityTokenException>(() => Validar(token, otro.ClavesValidacion()));
    }
}
