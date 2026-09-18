namespace Domain.Payloads;

public class CompraQueryParams : PaginationPayload
{
    public string? Value { get; set; }
    public int? ProveedorId { get; set; }
    public int? SucursalId { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}