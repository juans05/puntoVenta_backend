namespace Domain.Entities;

// App de la suite contratada por una empresa ("arp" = PuntoVenta, "arm" = CRM).
// Empresa sin filas = solo "arp" (todas las empresas anteriores a la suite).
public class TenantApp
{
    public int Id { get; set; }
    public int TenantIdentificador { get; set; }
    public string App { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public DateTime Desde { get; set; } = DateTime.UtcNow;
}
