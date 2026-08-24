namespace Domain.Entities;

public class Pais
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Idioma { get; set; } = null!;
    public string MonedaCodigo { get; set; } = null!;
    public string TimeZone { get; set; } = null!;
    public string EsquemaFiscal { get; set; } = null!;
    public ICollection<Moneda> Monedas { get; set; } = new List<Moneda>();
}