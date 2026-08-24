namespace Domain.Payloads
{
    public class ComprobanteQueryParams : PaginationPayload
    {
        public string? Value { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }
}
