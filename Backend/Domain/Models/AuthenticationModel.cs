using System.Runtime.Serialization;
using System.Text.Json.Serialization;
namespace Domain.Models
{
    [DataContract]
    public class AuthenticationModel
    {
        [IgnoreDataMember]
        public string MessageData { get; set; } = "Succeeded";

        [DataMember(Name = "isAuthenticated")]
        public bool IsAuthenticated { get; set; } = false;

        [DataMember(Name = "username")]
        public string UserName { get; set; }

        [DataMember(Name = "token")]
        public string Token { get; set; }

        [DataMember(Name = "tokenExpirationAt")]
        public string TokenExpirationAt { get; set; }

        [DataMember(Name = "tokenExpirationTimestamp")]
        public long TokenExpirationTimestamp { get; set; }

        [DataMember(Name = "tokenExpirationTimestampShort")]
        public long TokenExpirationTimestampShort { get; set; }




        [DataMember(Name = "refreshToken")]
        public string RefreshToken { get; set; }

        [DataMember(Name = "refreshTokenExpirationAt")]
        public string RefreshTokenExpirationAt { get; set; }

        [DataMember(Name = "refreshTokenExpirationTimestamp")]
        public long RefreshTokenExpirationTimestamp { get; set; }

        [DataMember(Name = "refreshTokenExpirationTimestampShort")]
        public long RefreshTokenExpirationTimestampShort { get; set; }

        [JsonIgnore]//-----------------------
        public DateTime RefreshTokenExpiration { get; set; }

        [JsonIgnore]//-----------------------
        public string ErrorMessage { get; set; }

        // Empresas a las que el usuario tiene acceso; si son varias, el front muestra el selector.
        [DataMember(Name = "empresas")]
        public List<Domain.DTO.EmpresaAccesoDto> Empresas { get; set; } = new();

        [DataMember(Name = "empresaActivaId")]
        public int EmpresaActivaId { get; set; }

    }

}
