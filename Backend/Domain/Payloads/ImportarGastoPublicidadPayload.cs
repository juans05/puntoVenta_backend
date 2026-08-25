namespace Domain.Payloads;

public class ImportarGastoPublicidadPayload
{
    public Guid LoteImportacionId { get; set; }
    public List<GastoPublicidadFilaPayload> Filas { get; set; } = new();
}
