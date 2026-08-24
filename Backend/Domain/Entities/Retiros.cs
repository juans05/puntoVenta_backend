
namespace Domain.Entities;

public class Retiros : EntityBase
{
    public int? SucursalId { get; set; }
    public int CajaId { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; }
    public Caja Caja { get; set; }
}
