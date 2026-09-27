namespace Domain.DTO;

public class AsientoContableDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public string Glosa { get; set; } = null!;
    public string OrigenTipo { get; set; } = null!;
    public int OrigenId { get; set; }
    public string EstadoAsiento { get; set; } = null!;
    public List<AsientoContableDetalleDto> Detalle { get; set; } = new();
}

public class AsientoContableDetalleDto
{
    public string CuentaCodigo { get; set; } = null!;
    public string CuentaNombre { get; set; } = null!;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
}
