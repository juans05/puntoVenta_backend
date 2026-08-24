namespace Domain.Entities.Identity;

public class AspNetModule : EntityBase
{
    public string Identificador { get; set; }
    public string Nombre { get; set; }
    public List<AspNetSubModule> Submodules { get; set; } = new List<AspNetSubModule>();
}