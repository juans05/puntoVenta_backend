namespace Domain.Entities;

public class EmpresaTenant
{
    public int EmpresaId { get; set; }
    public int TenantId { get; set; }

    public string? UsuarioCreacion { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public bool? Estado { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;

}
