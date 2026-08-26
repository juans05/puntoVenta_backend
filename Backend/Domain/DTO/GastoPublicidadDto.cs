namespace Domain.DTO;

public class GastoPublicidadDto
{
    public int Id { get; set; }
    public int? GrupoId { get; set; }
    public string? NombreGrupo { get; set; }
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
}
