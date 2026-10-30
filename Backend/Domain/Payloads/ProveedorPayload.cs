namespace Domain.Payloads
{
    public class ProveedorPayload : PaginationPayload
    {
        public string? Value { get; set; }
        // Filtros parciales del buscador de proveedores (popup en Compras); si viene alguno se ignora Value.
        public string? Nombre { get; set; }
        public string? NumeroDocumento { get; set; }
    }
}
