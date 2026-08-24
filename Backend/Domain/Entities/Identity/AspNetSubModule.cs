namespace Domain.Entities.Identity;

public class AspNetSubModule : EntityBase
{
    public string Identificador { get; set; }
    public string Nombre { get; set; }
    public string ModuloId { get; set; }
    public AspNetModule Module { get; set; } = null!;
    public List<AspNetUserSubModule> UserSubmodules { get; set; } = new List<AspNetUserSubModule>();
}