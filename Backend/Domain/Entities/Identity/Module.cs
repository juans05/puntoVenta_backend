namespace Domain.Entities.Identity;

public class Module : EntityBase
{
    public string Identificador { get; set; }
    public string Nombre { get; set; }
    public List<Submodule> Submodules { get; set; } = new List<Submodule>();
}