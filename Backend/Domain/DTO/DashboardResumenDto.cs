namespace Domain.DTO;

public class DashboardResumenDto
{
    public decimal VentasHoy { get; set; }
    public decimal GastosHoy { get; set; }
    public decimal ComprasHoy { get; set; }
    public decimal OtrosIngresosHoy { get; set; }
    public decimal CostoVentasHoy { get; set; }
    public decimal UtilidadEstimada { get; set; }
    public decimal FlujoCaja { get; set; }
    public decimal SaldoEsperado { get; set; }
    public int StockTotal { get; set; }
    public int ProductosStockBajo { get; set; }
    public List<TendenciaDiaDto> TendenciaDiaria { get; set; } = new();
    public List<ProductoTopDto> ProductosMasVendidos { get; set; } = new();
    public List<CategoriaMontoDto> GastosPorCategoria { get; set; } = new();
    public List<DistritoVentaDto> VentasPorDistrito { get; set; } = new();
    public List<CategoriaMontoDto> VentasPorTipoEnvio { get; set; } = new();
    public List<string> Alertas { get; set; } = new();
}

public class TendenciaDiaDto
{
    public string Fecha { get; set; } = null!;
    public decimal Ventas { get; set; }
    public decimal Compras { get; set; }
    public decimal Gastos { get; set; }
    public decimal CostoVentas { get; set; }
    public decimal Utilidad => Ventas - CostoVentas - Gastos;
}

public class CategoriaMontoDto
{
    public string Categoria { get; set; } = null!;
    public decimal Total { get; set; }
}

public class DistritoVentaDto
{
    public string Distrito { get; set; } = null!;
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
}

public class ProductoTopDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
    public decimal Costo { get; set; }
    public decimal Utilidad => Total - Costo;
    public decimal MargenPorcentaje => Total == 0 ? 0 : Math.Round(Utilidad / Total * 100, 2);
}

public class ReporteMargenDto
{
    public string FechaInicio { get; set; } = null!;
    public string FechaFin { get; set; } = null!;
    public decimal TotalVentas { get; set; }
    public decimal TotalCosto { get; set; }
    public decimal TotalUtilidad => TotalVentas - TotalCosto;
    public decimal MargenPorcentaje => TotalVentas == 0 ? 0 : Math.Round(TotalUtilidad / TotalVentas * 100, 2);
    public List<ProductoTopDto> Productos { get; set; } = new();
}