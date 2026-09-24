namespace Domain.Payloads
{
    public class ComprobantePayload
    {
        public int? ClienteId { get; set; }
        public int TipoDocumentoVentaId { get; set; }
        public int? TipoDocumentoId { get; set; } // Tipo de documento de identidad del cliente (DNI/RUC/...), no confundir con TipoDocumentoVentaId
        public string? NumeroDocumento { get; set; }
        public string? RazonSocial { get; set; }
        public string? DireccionCliente { get; set; }
        public string? UbigeoId { get; set; }
        public string? Celular { get; set; }
        public string? Email { get; set; }

        public decimal Total { get; set; }
        public DateTime? FechaVenta { get; set; }
        public bool? EsEcommerce { get; set; }
        public string? TipoEnvio { get; set; }
        public string? Distrito { get; set; }
        public bool EsCredito { get; set; }
        public decimal? PorcentajeDescuento { get; set; }
        public decimal? MontoDescuento { get; set; }
        public decimal? MontoRecibido { get; set; }
        public decimal? Vuelto { get; set; }
        public string? Observacion { get; set; }
        public char? ModoEnvio { get; set; } // EstatusModoEnvio: F=Solo Firmar e Imprimir, S=Enviar a SUNAT ahora, G=Solo Guardar

        // Campos de "Opc. Avanzadas" (ver ComprobanteCabecera) -- mismo nombre en ambos lados
        // para que el automapper (MyAutomapper.CreateMap<ComprobantePayload, ComprobanteCabecera>) los copie solo.
        public int? SucursalId { get; set; }
        public int? TipoOperacionId { get; set; }
        public string? PlacaVehiculo { get; set; }
        public string? GuiaRemisionManual { get; set; }
        public string? GuiaRemisionElectronica { get; set; }
        public string? Etiquetas { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string? NumeroOrden { get; set; }
        public string? ColaboradorId { get; set; }
        public int? MonedaId { get; set; }
        public decimal? TipoCambio { get; set; }
        public decimal? MontoRetencion { get; set; }
        public decimal? MontoAnticipo { get; set; }

        // Solo Cotizacion (TipoDocumentoVentaId = 6) usa FechaVigencia. CotizacionOrigenId lo
        // manda la Factura/Boleta resultante cuando se emite convirtiendo una cotizacion.
        public DateTime? FechaVigencia { get; set; }
        public int? CotizacionOrigenId { get; set; }

        // Solo flujo de ventas COMPLETO: factura/boleta de lo entregado de un pedido de venta.
        public int? PedidoVentaId { get; set; }

        public List<ComprobanteDetallePayload> DetalleComprobante { get; set; } = new List<ComprobanteDetallePayload>();
        public List<PagoPayload> DetallePago { get; set; } = new List<PagoPayload>();

    }

    public class ComprobanteDetallePayload
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal? CostoReal { get; set; }
        public int? TipoIgvId { get; set; }
        public int? UnidadMedidaId { get; set; }
    }

    public class PagoPayload
    {
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
    }
}
