namespace Domain.DTO;

public class IntentResult
{
    public string Intencion { get; set; } = null!;
    public string Respuesta { get; set; } = null!;
    public bool RequiereConfirmacion { get; set; }
    public string? PayloadJson { get; set; }
}