namespace Domain.Payloads
{
    public class ComprobanteQueryParams : PaginationPayload
    {
        public string? Value { get; set; }
        // StartDate/EndDate filtran por FechaVenta (o FechaCreacion si no hay FechaVenta) --
        // en la practica es la "fecha de documento", ya que casi siempre coinciden salvo que
        // el comprobante se haya emitido con fecha de venta distinta a la de registro.
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        // Fecha de registro: filtra estrictamente por FechaCreacion (cuando se guardo el
        // registro en el sistema), independiente de la fecha de venta/documento.
        public string? FechaRegistroInicio { get; set; }
        public string? FechaRegistroFin { get; set; }
        public string? NumeroDocumento { get; set; }
    }
}
