using Domain.Entities.Identity;

namespace Domain.Entities;

public class Tenant
{
    public int Identificador { get; set; }
    public string Name { get; set; } = null!;
    public string TenantKey { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public List<User> Users { get; set; } = new List<User>();
    public List<EmpresaTenant> EmpresaTenants { get; set; } = new List<EmpresaTenant>();
    public List<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
    //Se agrego rubro
    public int RubroId { get; set; }
    public int? PaisId { get; set; }
    public int? MonedaId { get; set; }

    public Rubro Rubro { get; set; } = null!;
    public Pais? Pais { get; set; }
    public Moneda? Moneda { get; set; }

}