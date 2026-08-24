namespace Domain.DTO;

public class TenantResumenDto
{
    public int Identificador { get; set; }
    public string Name { get; set; } = null!;
    public string TenantKey { get; set; } = null!;
    public bool Activo { get; set; }
    public int RubroId { get; set; }
    public string? RubroNombre { get; set; }
    public string? NombreComercial { get; set; }
    public string? Ruc { get; set; }
}
