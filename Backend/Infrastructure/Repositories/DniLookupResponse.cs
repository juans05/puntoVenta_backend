using Newtonsoft.Json;

namespace Infrastructure.Repositories;

// Forma de la respuesta del webhook n8n de consulta DNI -- ver ExtensionesRepository.ConsultarDni.
public class DniLookupResponse
{
    [JsonProperty("pac_numero_documento")]
    public string NumeroDocumento { get; set; }

    [JsonProperty("pac_nombre")]
    public string Nombre { get; set; }

    [JsonProperty("pac_apellido_paterno")]
    public string ApellidoPaterno { get; set; }

    [JsonProperty("pac_apellido_materno")]
    public string ApellidoMaterno { get; set; }

    [JsonProperty("pac_fecha_nacimiento")]
    public DateTime? FechaNacimiento { get; set; }

    [JsonProperty("pac_telefono_movil")]
    public string TelefonoMovil { get; set; }
}
