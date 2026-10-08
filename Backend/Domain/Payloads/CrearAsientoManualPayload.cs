namespace Domain.Payloads;

// Asiento manual (pestaña "Asiento contable"): Fecha = contabilizacion; Glosa = descripcion general.
public class CrearAsientoManualPayload
{
    public DateTime Fecha { get; set; }
    public DateTime? FechaDocumento { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string Glosa { get; set; } = null!;
    public List<LineaAsientoContable> Lineas { get; set; } = new();
}
