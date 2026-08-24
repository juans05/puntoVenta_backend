namespace Domain.Entities;

public class Ingreso : EntityBase
{
    public int? SucursalId { get; set; }
    public string Tipo { get; set; } = null!;
    public decimal Monto { get; set; }
    public int? MetodoPagoId { get; set; }
    public Metodopago? Metodopago { get; set; }
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public DateTime FechaIngreso { get; set; }
}