
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;

public class Role : IdentityRole<string>
{
    public List<UserRol> UserRoles { get; set; } = new List<UserRol>();
}
