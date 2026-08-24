namespace Domain.Entities;

public class Gasto : EntityBase
{
    public int? SucursalId { get; set; }
    public string Categoria { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public Metodopago? Metodopago { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public DateTime FechaGasto { get; set; }
}