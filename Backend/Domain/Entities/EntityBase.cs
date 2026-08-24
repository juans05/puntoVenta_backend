namespace Domain.Entities;

public abstract class EntityBase
{
    public EntityBase()
    {
        Estado = true;
    }
    public int Id { get; set; }

    public string? UsuarioCreacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool Estado { get; set; }

    public string TenantId { get; set; } = null!;
}
