namespace Domain.Entities;

public class GastoPublicidad : EntityBase
{
    // Nullable: un anuncio puede no corresponder a ningún grupo de productos
    // (ej. campañas de branding general) — se marca explícitamente como "No aplica".
    public int? GrupoId { get; set; }
    public Grupo? Grupo { get; set; }

    // true = el anuncio fue marcado como "No va" por el usuario → se excluye del cálculo de ROI.
    public bool Descartado { get; set; }

    public string NombreAnuncio { get; set; } = null!;
    public string? NombreConjuntoAnuncios { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public decimal ImporteGastado { get; set; }

    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Resultados { get; set; }
    public decimal? CostoPorResultado { get; set; }
    public int? Clics { get; set; }
    public decimal? CostoPorClic { get; set; }

    public Guid LoteImportacionId { get; set; }

    public string HashAnuncio { get; set; } = null!;
}
