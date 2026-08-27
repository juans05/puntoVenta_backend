namespace Domain.Entities.Identity;

public class RoleSubmodule : EntityBase
{
    public string RoleId { get; set; } = null!;
    public string SubmoduleId { get; set; } = null!;

    public Role Role { get; set; } = null!;
    public AspNetSubModule Submodule { get; set; } = null!;
}
