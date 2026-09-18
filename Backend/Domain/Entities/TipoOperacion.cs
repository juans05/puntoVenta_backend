namespace Domain.Entities;

public class TipoOperacion : EntityBase
{
    public string Codigo { get; set; } = null!;       // SUNAT tabla 17, simplificado a los codigos de uso comun en POS
    public string Descripcion { get; set; } = null!;
}
