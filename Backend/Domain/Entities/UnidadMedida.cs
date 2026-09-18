namespace Domain.Entities;

public class UnidadMedida : EntityBase
{
    public string Codigo { get; set; } = null!;       // "NIU","ZZ","KGM",... (SUNAT UN/ECE rec 20)
    public string Descripcion { get; set; } = null!;
}
