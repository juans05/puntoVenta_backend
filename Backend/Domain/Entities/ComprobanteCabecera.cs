using Domain.Enumerations;

namespace Domain.Entities;

public class ComprobanteCabecera : EntityBase
{
    public int? SucursalId { get; set; }
    public int TipoDocumentoVentaId { get; set; }
    public int? ClienteId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? RazonSocial { get; set; }

    public string Serie { get; set; } = null!;
    public int Correlativo { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorSubtotal { get; set; }
    public decimal ValorIgv { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public string? TotalLetras { get; set; } = null!;
    public char EstadoComprobante { get; set; } = EstatusComprobante.Creado;
    public DateTime? FechaVenta { get; set; }
    public char EnviadoSunat { get; set; } = EstatusEnvioSunat.Pendiente;
    public char? ModoEnvio { get; set; } // EstatusModoEnvio: F=Solo Firmar e Imprimir, S=Enviar a SUNAT ahora, G=Solo Guardar
    public string? MensajeSunat { get; set; }
    public string? MotivoAnulacion { get; set; }

    public bool EnvioAnulacionSunat { get; set; }
    public string? TicketSunat { get; set; }

    public bool? EsEcommerce { get; set; }
    public string? TipoEnvio { get; set; }
    public string? Distrito { get; set; }

    public int? ComprobanteAfectadoId { get; set; }
    public int? MotivoNotaId { get; set; }

    // Solo aplica a Cotizacion (TipoDocumentoVentaId = 6): hasta cuando es valida (no se persiste
    // un estado "Vencida" -- se calcula comparando contra la fecha actual) y, si el cliente acepta,
    // el Id de la Factura/Boleta en la que se convirtio (se completa en el comprobante resultante,
    // no en la cotizacion en si).
    public DateTime? FechaVigencia { get; set; }
    public int? CotizacionOrigenId { get; set; }

    public bool EsCredito { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public decimal? MontoDescuento { get; set; }
    public decimal? MontoRecibido { get; set; }
    public decimal? Vuelto { get; set; }
    public string? Observacion { get; set; }

    // Campos de "Opc. Avanzadas": todos opcionales, se completan solo si el usuario
    // habilita el campo correspondiente en el formulario.
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

    public Cliente? Cliente { get; set; }
    public TipoDocumentoVenta TipoDocumentoVenta { get; set; } = null!;
    public ComprobanteCabecera? ComprobanteAfectado { get; set; }
    public ComprobanteCabecera? CotizacionOrigen { get; set; }
    public MotivoNota? MotivoNota { get; set; }
    public TipoOperacion? TipoOperacion { get; set; }
    public Moneda? Moneda { get; set; }
    public List<ComprobanteDetalle> ComprobanteDetalles { get; set; } = new List<ComprobanteDetalle>();
    public List<Pago> Pagos { get; set; } = new List<Pago>();
}


