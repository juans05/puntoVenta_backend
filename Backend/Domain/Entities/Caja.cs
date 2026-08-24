namespace Domain.Entities;

public class Caja : EntityBase
{
    public int? SucursalId { get; set; }
    public decimal MontoInicio { get; set; }
    public decimal MontoCierre { get; set; }
    public DateTime? FechaHoraCierre { get; set; }
    public List<Pago> Pagos { get; set; } = new();
    public List<Retiros> Retiros { get; set; } = new();
}
