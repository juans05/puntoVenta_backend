namespace Domain.Payloads;

public class CrearNotaCompraPayload
{
    public int CompraId { get; set; }
    public string Tipo { get; set; } = null!; // TipoNotaCompra.Credito / .Debito
    public int MotivoNotaId { get; set; }
    public string? Observacion { get; set; }
}
