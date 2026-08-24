namespace Domain.Payloads;

public class CreateAjusteInventarioPayload
{
    public int ProductoId { get; set; }

    public int TipoMovimiento { get; set; }

    public int Cantidad { get; set; }

    public string? Motivo { get; set; }
}