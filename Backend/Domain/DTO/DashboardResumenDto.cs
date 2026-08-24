namespace Domain.DTO;

public class DashboardResumenDto
{
    public decimal VentasHoy { get; set; }
    public decimal GastosHoy { get; set; }
    public decimal ComprasHoy { get; set; }
    public decimal OtrosIngresosHoy { get; set; }
    public decimal CostoVentasHoy { get; set; }
    public decimal UtilidadEstimada { get; set; }
    public decimal SaldoEsperado { get; set; }
    public int StockTotal { get; set; }
    public int ProductosStockBajo { get; set; }
    public List<VentaDiaDto> VentasUltimos7Dias { get; set; } = new();
    public List<ProductoTopDto> ProductosMasVendidos { get; set; } = new();
    public List<string> Alertas { get; set; } = new();
}

public class VentaDiaDto
{
    public string Fecha { get; set; } = null!;
    public decimal Total { get; set; }
}

public class ProductoTopDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
    public decimal Costo { get; set; }
    public decimal Utilidad => Total - Costo;
}