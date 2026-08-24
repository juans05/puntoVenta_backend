namespace Domain.Entities;

public class Moneda
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Simbolo { get; set; } = null!;
    public string Locale { get; set; } = null!;
    public int PaisId { get; set; }
    public Pais Pais { get; set; } = null!;
    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
}