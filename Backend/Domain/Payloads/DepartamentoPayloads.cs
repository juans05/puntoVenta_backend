namespace Domain.Payloads;

public class CrearDepartamentoPayload
{
    public string Nombre { get; set; } = null!;
    // Ids de usuario que quedan como aprobadores del departamento (reemplaza la lista completa).
    public List<string> AprobadorIds { get; set; } = new();
}

public class AsignarAprobadoresPayload
{
    public List<string> AprobadorIds { get; set; } = new();
}
