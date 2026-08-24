namespace Domain.Entities.Identity;

public class UserSubmodule : EntityBase
{
    public string UserId { get; set; }
    public string SubmoduleId { get; set; }

    public User User { get; set; } = null!;
    public Submodule Submodule { get; set; } = null!;

}
