namespace Domain.Entities;

public class AuditLog : EntityBase
{
    public string Accion { get; set; } = null!;

    public string Entidad { get; set; } = null!;

    public int? EntidadId { get; set; }

    public string? Valores { get; set; }
}