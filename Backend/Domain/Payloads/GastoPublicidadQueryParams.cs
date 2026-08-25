namespace Domain.Payloads;

public class GastoPublicidadQueryParams : PaginationPayload
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ProductoId { get; set; }
}
