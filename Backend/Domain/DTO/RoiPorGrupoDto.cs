namespace Domain.DTO;

public class RoiPorGrupoDto
{
    public int? GrupoId { get; set; }
    public string? NombreGrupo { get; set; }
    public decimal GastoAds { get; set; }
    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Clics { get; set; }
    public decimal? CostoPorClic { get; set; }
    public decimal Ingresos { get; set; }
    public decimal CostoProducto { get; set; }
    public decimal UtilidadNeta { get; set; }
    public decimal? RoiPorcentaje { get; set; }
}
