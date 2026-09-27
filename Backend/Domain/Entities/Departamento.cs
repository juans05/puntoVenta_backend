using Domain.Entities.Identity;

namespace Domain.Entities;

// Jefatura que aprueba ordenes de compra/servicio sobre el umbral (ConfiguracionFlujo.MontoAprobacionOc).
public class Departamento : EntityBase
{
    public string Nombre { get; set; } = null!;
    public List<DepartamentoAprobador> Aprobadores { get; set; } = new();
}

// Quienes pueden aprobar en ese departamento (N a N: un usuario puede ser aprobador de varios).
public class DepartamentoAprobador : EntityBase
{
    public int DepartamentoId { get; set; }
    public Departamento? Departamento { get; set; }
    public string UserId { get; set; } = null!;
    public User? User { get; set; }
}
