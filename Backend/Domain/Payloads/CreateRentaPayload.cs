namespace Domain.Payloads;

public class CreateRentaPayload
{
    public int HabitacionId { get; set; }

    public int AnfitrionaId { get; set; }

    public string Turno { get; set; }

    public decimal TarifaCuarto { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoCuarto { get; set; }
    public decimal MontoPendiente { get; set; }

    public string? Observaciones { get; set; }

    public List<RentaDetallePayload> DetalleProductos { get; set; }
}

public class RentaDetallePayload
{
    public int ProductoId { get; set; }

    public decimal Precio { get; set; }
}