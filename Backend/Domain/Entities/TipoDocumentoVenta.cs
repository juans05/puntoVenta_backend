namespace Domain.Entities
{
    public class TipoDocumentoVenta : EntityBase //FACTURA, BOLETA
    {
        public string Nombre { get; set; } = null!;

        public List<ComprobanteCabecera> ComprobanteCabeceras { get; set; } = new List<ComprobanteCabecera>();
        public List<Seriecorrelativo> Seriecorrelativos { get; set; } = new List<Seriecorrelativo>();
    }
}
