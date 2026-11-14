namespace Domain.Payloads;

public class CreateMetodoPagoPayload
{
    public string Nombre { get; set; } = null!;
}

public class ActualizarMetodoPagoPayload
{
    public string Nombre { get; set; } = null!;
    public int? CuentaContableId { get; set; }
}
