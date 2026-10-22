namespace Domain.Payloads;

// Empresa a la que el dueño da acceso a un usuario; RoleId null = administrador (acceso completo).
public class EmpresaUsuarioPayload
{
    public int TenantId { get; set; }
    public string? RoleId { get; set; }
}
