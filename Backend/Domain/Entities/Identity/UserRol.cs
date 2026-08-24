using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;


public class UserRol : IdentityUserRole<string>
{
    public Role Role { get; set; } = null!;
    public User User { get; set; } = null!;
}
