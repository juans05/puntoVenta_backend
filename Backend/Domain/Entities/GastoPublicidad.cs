namespace Domain.Entities;

public class GastoPublicidad : EntityBase
{
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;

    public string NombreAnuncio { get; set; } = null!;
    public string? NombreConjuntoAnuncios { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public decimal ImporteGastado { get; set; }

    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Resultados { get; set; }
    public decimal? CostoPorResultado { get; set; }

    public Guid LoteImportacionId { get; set; }

    public string HashAnuncio { get; set; } = null!;
}
