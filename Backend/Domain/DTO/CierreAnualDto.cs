namespace Domain.DTO;

public class CierreAnualChecklistItemDto
{
    public string Descripcion { get; set; } = null!;
    public bool Ok { get; set; }
    public string? Detalle { get; set; }
}

public class CierreAnualValidacionDto
{
    public bool PuedeCerrar { get; set; }
    public List<CierreAnualChecklistItemDto> Checklist { get; set; } = new();
}

public class CierreAnualFaseDto
{
    public int Fase { get; set; }
    public string Nombre { get; set; } = null!;
    public string? AsientoNumero { get; set; }
    public decimal Total { get; set; }
}

public class CierreAnualResultadoDto
{
    public decimal ResultadoAntesDeImpuestos { get; set; }
    public decimal ImpuestoRenta { get; set; }
    public decimal ResultadoNeto { get; set; }
    public List<CierreAnualFaseDto> Fases { get; set; } = new();
}
