namespace Domain.Entities;

public class TipoIgv : EntityBase
{
    public string Codigo { get; set; } = null!;       // "10","20","30" (SUNAT tabla 07)
    public string Descripcion { get; set; } = null!;
    public bool AplicaPorcentajeImpuesto { get; set; } // true=Gravado (usa el % del tenant), false=Exonerado/Inafecto (0%)
}
