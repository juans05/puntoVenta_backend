
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;

public class Role : IdentityRole<string>
{
    public string? TenantId { get; set; }
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; }

    public List<UserRol> UserRoles { get; set; } = new List<UserRol>();
    public List<RoleSubmodule> RoleSubmodules { get; set; } = new List<RoleSubmodule>();
}
