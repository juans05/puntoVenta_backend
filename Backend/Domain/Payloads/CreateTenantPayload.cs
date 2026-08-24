namespace Domain.Payloads;

public class CreateTenantPayload
{
    public string Nombre { get; set; }
    public int RubroId { get; set; }
    public ConfiguracionRentaPayload? Configuracion { get; set; }
}
