namespace Domain.Payloads;

public class SubmitPedidoResult
{
    public bool Exito { get; set; }
    public string? PasswordGenerado { get; set; }
    public int PedidoId { get; set; }
    public string? Celular { get; set; }
    public bool CuentaExistia { get; set; }
}