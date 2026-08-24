using Domain.Entities.Identity;

namespace Domain.Entities;

public class Sucursal : EntityBase
{
    public string Nombre { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? UbigeoId { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public int MonedaId { get; set; }
    public int PaisId { get; set; }
    public int RubroId { get; set; }

    public Moneda Moneda { get; set; } = null!;
    public Pais Pais { get; set; } = null!;
    public Rubro Rubro { get; set; } = null!;
    public List<User> Users { get; set; } = new List<User>();
}