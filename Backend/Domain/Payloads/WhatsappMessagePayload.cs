namespace Domain.Payloads;

public class WhatsappMessagePayload
{
    public string MessageId { get; set; } = null!;
    public string Numero { get; set; } = null!;
    public string Texto { get; set; } = null!;
    public string? Username { get; set; }
    public int? SucursalId { get; set; }
}