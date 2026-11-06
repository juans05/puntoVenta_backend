using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identidad;

// Firma de los tokens del staff. Con clave RSA configurada firma RS256 y publica la clave publica
// (JWKS) para que otras apps de la suite validen sin poder emitir tokens. Sin RSA sigue en HMAC.
// La validacion acepta ambas claves para que los tokens HMAC ya emitidos vivan hasta expirar.
public class FirmadorToken
{
    private readonly SymmetricSecurityKey _hmac;
    private readonly RsaSecurityKey? _rsa;

    public FirmadorToken(string secretKeyHmac, string? rsaPrivateKeyPem)
    {
        _hmac = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKeyHmac));

        if (string.IsNullOrWhiteSpace(rsaPrivateKeyPem)) return;

        var rsa = RSA.Create();
        try
        {
            // Variables de entorno suelen traer el PEM en una linea con "\n" escapados y/o entre comillas.
            rsa.ImportFromPem(rsaPrivateKeyPem.Trim().Trim('"').Replace("\\n", "\n"));
            // Una clave publica importa sin error pero no firma: cada login daria 500.
            rsa.ExportParameters(true);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new InvalidOperationException(
                "TokenManagement:RsaPrivateKeyPem no contiene una clave privada RSA en formato PEM valido.", ex);
        }
        _rsa = new RsaSecurityKey(rsa) { KeyId = KeyIdDe(rsa) };
    }

    public SigningCredentials Credenciales() => _rsa != null
        ? new SigningCredentials(_rsa, SecurityAlgorithms.RsaSha256)
        : new SigningCredentials(_hmac, SecurityAlgorithms.HmacSha256Signature);

    public IEnumerable<SecurityKey> ClavesValidacion() => _rsa != null
        ? new SecurityKey[] { _hmac, _rsa }
        : new SecurityKey[] { _hmac };

    public JsonWebKeySet Jwks()
    {
        var set = new JsonWebKeySet();
        if (_rsa == null) return set;

        var publica = new RsaSecurityKey(_rsa.Rsa.ExportParameters(false)) { KeyId = _rsa.KeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publica);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        set.Keys.Add(jwk);
        return set;
    }

    // Estable entre reinicios: el "kid" del token coincide con el del JWKS mientras no cambie la clave.
    private static string KeyIdDe(RSA rsa)
        => Convert.ToHexString(SHA256.HashData(rsa.ExportParameters(false).Modulus!))[..16].ToLowerInvariant();
}
