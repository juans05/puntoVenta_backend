namespace Domain.DTO;

public class RoiPorProductoDto
{
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public decimal GastoAds { get; set; }
    public decimal Ingresos { get; set; }
    public decimal CostoProducto { get; set; }
    public decimal UtilidadNeta { get; set; }
    public decimal? RoiPorcentaje { get; set; }
}
