using Domain.Entities.Identity;

namespace Domain.Entities;

// Acceso de un usuario a una empresa (tenant). Un usuario puede trabajar en varias empresas y elige
// la activa al iniciar sesion o desde el header; el token se emite para esa empresa.
// EsPropietario: el dueño de la cuenta -- puede crear empresas y dar acceso a otros usuarios.
// El cobro es por usuario: los usuarios de una cuenta son los distintos con acceso a sus empresas.
public class UsuarioEmpresa
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public bool EsPropietario { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}
