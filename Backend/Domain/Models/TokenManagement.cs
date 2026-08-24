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

    }
}
