namespace Domain.Entities;

public class ConfiguracionRenta : EntityBase
{
    public int? SucursalId { get; set; }

    public int? RubroId { get; set; }

    public string? Tipo { get; set; }

    public string TurnosJson { get; set; } = "[]";

    public string TarifasJson { get; set; } = "[]";

    public string RecursosJson { get; set; } = "[]";
}