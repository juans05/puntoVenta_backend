namespace Domain.Entities;

public class WhatsappMessage : EntityBase
{
    public string MessageId { get; set; } = null!;
    public string NumeroOrigen { get; set; } = null!;
    public string Texto { get; set; } = null!;
    public string Direccion { get; set; } = "IN";
    public string? Intencion { get; set; }
    public new string Estado { get; set; } = "RECIBIDO";
    public string? Respuesta { get; set; }
}