namespace Domain.DTO;

public class NotaCompraDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public int CompraId { get; set; }
    public string? NumeroCompra { get; set; }
    public string Tipo { get; set; } = null!;
    public string? MotivoDescripcion { get; set; }
    public string Fecha { get; set; } = null!;
    public decimal Monto { get; set; }
    public string EstadoNota { get; set; } = null!;
    public string? Observacion { get; set; }
}
