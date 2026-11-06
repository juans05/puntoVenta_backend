namespace Domain.Models
{
    public class TokenManagement
    {
        public string SecretKey { get; set; }

        public string EncryptionSecret { get; set; }

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public int AccessTokenExpiration { get; set; }

        public int RefreshTokenExpiration { get; set; }

        public int DesfaceTimeWithServer { get; set; }

        public TimeSpan TokenLifeTime { get; set; }

        // PEM de la clave privada RSA para firmar RS256. Vacio = HMAC con SecretKey (comportamiento anterior).
        public string? RsaPrivateKeyPem { get; set; }

        // Audiencia de los tokens de clientes de la tienda: distinta a la del staff para que un
        // token de cliente no sirva en la API de administracion (y viceversa).
        public static string AudienciaCliente(string? audienceStaff) => $"{audienceStaff}/cliente";

    }
}
