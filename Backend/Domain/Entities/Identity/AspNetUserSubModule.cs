namespace Domain.Entities.Identity;

public class AspNetUserSubModule : EntityBase
{
    public string UserId { get; set; }
    public string SubmoduleId { get; set; }

    public User User { get; set; } = null!;
    public AspNetSubModule Submodule { get; set; } = null!;
}
