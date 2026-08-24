namespace Domain.Payloads;

public class CreateGastoPayload
{
    public string Categoria { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public string? Observacion { get; set; }
    public DateTime? FechaGasto { get; set; }
}