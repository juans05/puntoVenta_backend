namespace Domain.DTO;

public class ResumenDia
{
    public decimal SaldoInicial { get; set; }
    public decimal Ventas { get; set; }
    public decimal OtrosIngresos { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos { get; set; }
    public decimal Compras { get; set; }
    public decimal Retiros { get; set; }
    public decimal Egresos { get; set; }
    public decimal SaldoEsperado { get; set; }
}