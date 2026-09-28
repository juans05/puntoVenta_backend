namespace Domain.DTO;

public class LineaReporteContableDto
{
    public string CuentaCodigo { get; set; } = null!;
    public string CuentaNombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public decimal Monto { get; set; }
}

public class EstadoResultadosDto
{
    public List<LineaReporteContableDto> Ingresos { get; set; } = new();
    public List<LineaReporteContableDto> Gastos { get; set; } = new();
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal UtilidadNeta { get; set; }
}

public class BalanceGeneralDto
{
    public List<LineaReporteContableDto> Activo { get; set; } = new();
    public List<LineaReporteContableDto> Pasivo { get; set; } = new();
    public List<LineaReporteContableDto> Patrimonio { get; set; } = new();
    // Utilidad del ejercicio no distribuida (Ingresos - Gastos acumulados hasta la fecha), agregada
    // a Patrimonio como linea sintetica -- este sistema no maneja asientos de cierre de periodo.
    public decimal ResultadoDelEjercicio { get; set; }
    public decimal TotalActivo { get; set; }
    public decimal TotalPasivoYPatrimonio { get; set; }
}
