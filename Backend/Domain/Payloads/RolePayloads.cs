namespace Domain.Payloads;

public class CreateRolePayload
{
    public string Nombre { get; set; } = null!;
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; } = 100;
    public List<string> SubmoduleIds { get; set; } = new();
}

public class UpdateRolePayload
{
    public string Nombre { get; set; } = null!;
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; } = 100;
    public List<string> SubmoduleIds { get; set; } = new();
}

public class AsignarRolesUsuarioPayload
{
    public string UserId { get; set; } = null!;
    public List<string> RoleIds { get; set; } = new();
}
