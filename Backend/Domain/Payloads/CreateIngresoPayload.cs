namespace Domain.Payloads;

public class CreateIngresoPayload
{
    public string Tipo { get; set; } = null!;
    public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime? FechaIngreso { get; set; }
}