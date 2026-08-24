namespace Domain.Payloads;

public class AddEmpresaPayload
{
    public int identificador { get; set; }
    public int idEmpresa { get; set; }
    public string tenantNombre { get; set; }
}
