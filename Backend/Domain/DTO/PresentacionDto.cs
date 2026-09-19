namespace Domain.DTO;

public class PresentacionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Codigo { get; set; }
    public int UnidadMedidaId { get; set; }
    public string? UnidadMedidaNombre { get; set; }
    public decimal Factor { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal? PrecioMinimo { get; set; }
}
