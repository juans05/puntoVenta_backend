namespace Domain.Entities;

public class WhatsappConversation : EntityBase
{
    public string Numero { get; set; } = null!;
    public new string? Estado { get; set; }
    public string? ContextoJson { get; set; }
    public DateTime? UltimoMensaje { get; set; }
}