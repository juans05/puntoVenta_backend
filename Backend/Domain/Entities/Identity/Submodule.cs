namespace Domain.Entities.Identity;

public class Submodule : EntityBase
{
    public string Identificador { get; set; }
    public string Nombre { get; set; }
    public string ModuloId { get; set; }
    public Module Module { get; set; } = null!;
    public List<UserSubmodule> UserSubmodules { get; set; } = new List<UserSubmodule>();
}