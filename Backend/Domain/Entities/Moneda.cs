namespace Domain.Entities;

// EntityBase da TenantId (nullable): null = catalogo base SUNAT compartido por todos los
// tenants (los 4 seeds originales), no-null = moneda propia agregada por ese tenant via CRUD.
public class Moneda : EntityBase
{
    public string Codigo { get; set; } = null!;
    public string Simbolo { get; set; } = null!;
    public string Locale { get; set; } = null!;
    public int PaisId { get; set; }
    public Pais Pais { get; set; } = null!;
    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
}