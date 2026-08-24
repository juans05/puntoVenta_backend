namespace Domain.Entities;

public class Recurso : EntityBase
{
    public int? SucursalId { get; set; }

    public string Descripcion { get; set; } = null!;

    public int Zona { get; set; }

    public string? Tipo { get; set; }
}