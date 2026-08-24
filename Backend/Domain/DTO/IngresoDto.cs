namespace Domain.DTO;

public class IngresoDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = null!;
    public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public string? MetodoPago { get; set; }
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = null!;
    public string FechaIngreso { get; set; } = null!;
    public string? Usuario { get; set; }
}