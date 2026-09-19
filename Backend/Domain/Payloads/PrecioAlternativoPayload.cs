namespace Domain.Payloads;

public class PrecioAlternativoPayload
{
    public string Nombre { get; set; } = null!;
    public decimal PrecioVenta { get; set; }
}
