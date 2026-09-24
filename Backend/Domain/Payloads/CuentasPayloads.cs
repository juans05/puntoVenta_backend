namespace Domain.Payloads;

// Cobro a cliente / pago a proveedor: el mismo formato para ambos lados.
public class RegistrarPagoCuentaPayload
{
    public int SocioId { get; set; }
    public int MetodoPagoId { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public List<PagoCuentaLineaPayload> Detalle { get; set; } = new();
}

public class PagoCuentaLineaPayload
{
    public int DocumentoId { get; set; }
    public decimal Monto { get; set; }
}

public class PagosCuentaQueryParams
{
    public int Page { get; set; } = 1;
    public int Amount { get; set; } = 50;
    public int? SocioId { get; set; }
}
