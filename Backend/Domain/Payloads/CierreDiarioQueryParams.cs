namespace Domain.Payloads;

public class CierreDiarioQueryParams : PaginationPayload
{
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}