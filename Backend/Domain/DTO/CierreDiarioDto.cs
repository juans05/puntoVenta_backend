namespace Domain.DTO;

public class CierreDiarioDto
{
    public int Id { get; set; }
    public string FechaCierre { get; set; } = null!;
    public decimal SaldoInicial { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal SaldoEsperado { get; set; }
    public decimal SaldoReal { get; set; }
    public decimal Diferencia { get; set; }
    public string? Observaciones { get; set; }
    public string? Usuario { get; set; }
}