namespace Domain.Payloads
{
    public class UpdateClientePayload
    {
        public int ClienteId { get; set; }
        public string? Nombre { get; set; }
        public int? TipoDocumentoId { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Sexo { get; set; }
        public string FechaNacimiento { get; set; }
        public string UbigeoId { get; set; }

    }
}
