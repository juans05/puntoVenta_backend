namespace Domain.DTO;

public class RoleDto
{
    public string Id { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; }
    public int CantidadUsuarios { get; set; }
    public List<string> SubmoduleIds { get; set; } = new();
}
