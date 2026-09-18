namespace Domain.Payloads;

public class ContabilidadQueryParams
{
    public string? FechaInicio { get; set; }
    public string? FechaFin { get; set; }
    public int? SucursalId { get; set; }
}
