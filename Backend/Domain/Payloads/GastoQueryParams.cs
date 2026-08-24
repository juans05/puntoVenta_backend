namespace Domain.Payloads;

public class GastoQueryParams : PaginationPayload
{
    public string? Value { get; set; }
    public string? Categoria { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}