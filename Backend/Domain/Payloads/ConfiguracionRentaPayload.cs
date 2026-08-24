namespace Domain.Payloads;

public class ConfiguracionRentaPayload
{
    public string? Tipo { get; set; }

    public List<TurnoConfigPayload> Turnos { get; set; } = new List<TurnoConfigPayload>();

    public List<TarifaConfigPayload> Tarifas { get; set; } = new List<TarifaConfigPayload>();

    public List<RecursoConfigPayload> Recursos { get; set; } = new List<RecursoConfigPayload>();
}

public class TurnoConfigPayload
{
    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string HoraInicio { get; set; } = null!;

    public string HoraFin { get; set; } = null!;
}

public class TarifaConfigPayload
{
    public string Turno { get; set; } = null!;

    public string Dias { get; set; } = null!;

    public decimal Monto { get; set; }
}

public class RecursoConfigPayload
{
    public string Descripcion { get; set; } = null!;

    public int Zona { get; set; }

    public string? Tipo { get; set; }
}