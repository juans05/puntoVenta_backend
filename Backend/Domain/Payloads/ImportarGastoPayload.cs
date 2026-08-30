namespace Domain.Payloads;

public class GastoFilaPayload
{
    public DateTime FechaGasto { get; set; }
    public string Categoria { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? MetodoPago { get; set; }
    public decimal Monto { get; set; }
}

public class ImportarGastoPayload
{
    public List<GastoFilaPayload> Filas { get; set; } = new();
}
