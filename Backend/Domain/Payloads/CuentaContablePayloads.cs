namespace Domain.Payloads;

public class CrearCuentaContablePayload
{
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public int? CuentaPadreId { get; set; }
}

public class ActualizarCuentaContablePayload : CrearCuentaContablePayload
{
    public bool Estado { get; set; } = true;
}
