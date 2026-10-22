namespace Domain.DTO;

// Empresa a la que un usuario tiene acceso (selector de empresa del login y del header).
public class EmpresaAccesoDto
{
    public int TenantId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Ruc { get; set; }
    public bool EsPropietario { get; set; }
    // La empresa del token actual (la que el header muestra seleccionada).
    public bool Activa { get; set; }
}
