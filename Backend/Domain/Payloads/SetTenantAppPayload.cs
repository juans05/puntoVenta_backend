namespace Domain.Payloads;

public class SetTenantAppPayload
{
    public string App { get; set; } = null!;
    public bool Activo { get; set; }
}
