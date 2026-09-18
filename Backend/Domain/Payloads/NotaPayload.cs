namespace Domain.Payloads
{
    public class NotaPayload
    {
        public int ComprobanteAfectadoId { get; set; }
        public int MotivoNotaId { get; set; }
        public int TipoDocumentoVentaId { get; set; } // 4 = NotaCredito, 5 = NotaDebito
    }
}
