#nullable enable
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data;

// Cifrado en reposo (AES-GCM) para secretos de terceros guardados en la BD, ej. el token del
// proveedor de facturacion electronica. La clave sale de TokenManagement:EncryptionSecret.
// Formato guardado: "enc:v1:" + base64(nonce | tag | texto cifrado).
// Valores antiguos sin prefijo se leen tal cual y se cifran al volver a guardarse.
public static class CifradoDatos
{
    private const string Prefijo = "enc:v1:";
    private static byte[]? _clave;

    // Sin clave real (vacia o placeholder "YOUR_...") no se cifra: cifrar con un placeholder y
    // luego poner la clave real dejaria los tokens ilegibles.
    public static void Configurar(string? secreto)
    {
        _clave = string.IsNullOrWhiteSpace(secreto) || secreto.StartsWith("YOUR_")
            ? null
            : SHA256.HashData(Encoding.UTF8.GetBytes(secreto));
    }

    public static string? Cifrar(string? texto)
    {
        if (_clave is null || string.IsNullOrEmpty(texto) || texto.StartsWith(Prefijo)) return texto;
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plano = Encoding.UTF8.GetBytes(texto);
        var cifrado = new byte[plano.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_clave);
        aes.Encrypt(nonce, plano, cifrado, tag);
        return Prefijo + Convert.ToBase64String(nonce.Concat(tag).Concat(cifrado).ToArray());
    }

    // Si la clave cambio (rotacion) el valor ya no se puede leer: devuelve null y hay que volver
    // a ingresar el token en la configuracion fiscal.
    public static string? Descifrar(string? valor)
    {
        if (string.IsNullOrEmpty(valor) || !valor.StartsWith(Prefijo)) return valor;
        if (_clave is null) return null;
        try
        {
            var datos = Convert.FromBase64String(valor[Prefijo.Length..]);
            var plano = new byte[datos.Length - 28];
            using var aes = new AesGcm(_clave);
            aes.Decrypt(datos[..12], datos[28..], datos[12..28], plano);
            return Encoding.UTF8.GetString(plano);
        }
        catch (Exception e) when (e is CryptographicException or FormatException or ArgumentException)
        {
            return null;
        }
    }

    public static readonly ValueConverter<string?, string?> Conversor =
        new(v => Cifrar(v), v => Descifrar(v));
}
