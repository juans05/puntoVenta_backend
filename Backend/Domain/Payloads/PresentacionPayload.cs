namespace Domain.Payloads;

public class PresentacionPayload
{
    public string Nombre { get; set; } = null!;
    public string? Codigo { get; set; }
    public int UnidadMedidaId { get; set; }
    public decimal Factor { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal? PrecioMinimo { get; set; }
}
