namespace Domain.Entities;

public class Renta : EntityBase
{
    public int? SucursalId { get; set; }

    public int RecursoId { get; set; }

    public int AnfitrionaId { get; set; }

    public string Turno { get; set; } = null!;

    public DateTime FechaIngreso { get; set; }

    public DateTime? FechaSalida { get; set; }

    public decimal TarifaCuarto { get; set; }

    public decimal MontoTotal { get; set; }

    public decimal MontoCuarto { get; set; }

    public decimal MontoPendiente { get; set; }

    public string? Observaciones { get; set; }

    public Recurso? Recurso { get; set; }

    public Anfitriona? Anfitriona { get; set; }

    public List<RentaDetalle> Detalles { get; set; } = new List<RentaDetalle>();
}