namespace Domain.Payloads
{
    public class ClientePayload : PaginationPayload
    {
        public string? Value { get; set; }
        // Filtros del popup de busqueda (coincidencia parcial); si llega alguno se ignora Value.
        public string? Nombre { get; set; }
        public string? NumeroDocumento { get; set; }
    }
}
