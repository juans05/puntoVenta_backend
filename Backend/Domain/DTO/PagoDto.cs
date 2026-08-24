namespace Domain.DTO;

public class PagoDto
{
    public int CajaId { get; set; }
    public decimal MontoInicio { get; set; } = 0;
    public bool CajaAbierta { get; set; } = false;
    public decimal MontoCierre { get; set; } = 0;
    public decimal MontoEfectivo { get; set; } = 0;
    public decimal MontoTarjeta { get; set; } = 0;
    public decimal MontoYape { get; set; } = 0;

    public List<MetodoPagoMontoDto> MontosPorMetodo { get; set; } = new List<MetodoPagoMontoDto>();
    public decimal MontoRetiros => Retiros.Sum(x => x.Monto);
    public List<RetiroDto> Retiros { get; set; } = new List<RetiroDto>();
    public decimal MontoTotal => MontoInicio + MontoEfectivo + MontoTarjeta + MontoYape - MontoRetiros;
}

public class MetodoPagoMontoDto
{
    public int MetodoPagoId { get; set; }
    public string? Nombre { get; set; }
    public decimal Monto { get; set; }
}

public class RetiroDto
{
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = null!;
}
