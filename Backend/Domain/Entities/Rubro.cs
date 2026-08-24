namespace Domain.Entities;

public class Rubro
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
    public ICollection<RubroModulo> Moduulos { get; set; } = new List<RubroModulo>();
}