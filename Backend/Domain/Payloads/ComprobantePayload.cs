namespace Domain.Payloads
{
    public class ComprobantePayload
    {
        public int? ClienteId { get; set; }
        public int TipoDocumentoVentaId { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? RazonSocial { get; set; }

        public decimal Total { get; set; }
        public DateTime? FechaVenta { get; set; }
        public bool? EsEcommerce { get; set; }
        public string? TipoEnvio { get; set; }
        public string? Distrito { get; set; }
        public List<ComprobanteDetallePayload> DetalleComprobante { get; set; } = new List<ComprobanteDetallePayload>();
        public List<PagoPayload> DetallePago { get; set; } = new List<PagoPayload>();

    }

    public class ComprobanteDetallePayload
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal ValorUnitario { get; set; }
    }

    public class PagoPayload
    {
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
    }
}
