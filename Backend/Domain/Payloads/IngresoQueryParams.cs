namespace Domain.Payloads;

public class IngresoQueryParams : PaginationPayload
{
    public string? Value { get; set; }
    public string? Tipo { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}