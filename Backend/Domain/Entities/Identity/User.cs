using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;
public class User : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string FechaCreacion { get; set; }
    public bool Estado { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    public List<UserRol> UserRoles { get; set; } = new List<UserRol>();
    public List<AspNetUserSubModule> UserSubmodules { get; set; } = new List<AspNetUserSubModule>();

    public ICollection<AspNetUserToken> UserTokens { get; set; }

}
