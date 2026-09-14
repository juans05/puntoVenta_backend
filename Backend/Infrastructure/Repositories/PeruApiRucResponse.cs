using Newtonsoft.Json;

namespace Infrastructure.Repositories;

// Forma de la respuesta de https://peruapi.com/api/ruc/{ruc} -- ver ExtensionesRepository.ConsultarRuc.
public class PeruApiRucResponse
{
    public string Ruc { get; set; }

    [JsonProperty("razon_social")]
    public string RazonSocial { get; set; }

    public string Estado { get; set; }
    public string Condicion { get; set; }
    public string Direccion { get; set; }
    public string Ubigeo { get; set; }
    public string Mensaje { get; set; }
    public string Code { get; set; }
}
