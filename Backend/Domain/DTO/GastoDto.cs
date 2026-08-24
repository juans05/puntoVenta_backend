namespace Domain.DTO;

public class GastoDto
{
    public int Id { get; set; }
    public string Categoria { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public string? MetodoPago { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = null!;
    public string FechaGasto { get; set; } = null!;
    public string? Usuario { get; set; }
}