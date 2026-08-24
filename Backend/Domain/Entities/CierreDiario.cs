namespace Domain.Entities;

public class CierreDiario : EntityBase
{
    public int? SucursalId { get; set; }
    public DateTime FechaCierre { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal SaldoEsperado { get; set; }
    public decimal SaldoReal { get; set; }
    public decimal Diferencia { get; set; }
    public string? Observaciones { get; set; }
}