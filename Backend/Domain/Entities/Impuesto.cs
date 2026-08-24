namespace Domain.Entities;

public class Impuesto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal Porcentaje { get; set; }
    public string? AplicableA { get; set; }
    public int PaisId { get; set; }
    public Pais Pais { get; set; } = null!;
}