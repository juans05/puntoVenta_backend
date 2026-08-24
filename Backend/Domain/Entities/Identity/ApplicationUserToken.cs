
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities.Identity;

[Owned]
public class ApplicationUserToken
{
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public bool IsExpired => DateTime.UtcNow.AddHours(-5) >= Expires;
    public DateTime Created { get; set; }
    public DateTime? Revoked { get; set; }
    public bool IsActive => Revoked == null && !IsExpired;
}
