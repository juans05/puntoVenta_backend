namespace Domain.Entities;

public class RubroModulo
{
    public int Id { get; set; }
    public int RubroId { get; set; }
    public string CodigoModulo { get; set; } = null!;
    public bool Activo { get; set; } = true;

    public Rubro Rubro { get; set; } = null!;
}