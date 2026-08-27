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
    public string? MensajeSunat { get; set; }
    public string? MotivoAnulacion { get; set; }

    public bool EnvioAnulacionSunat { get; set; }
    public string? TicketSunat { get; set; }

    public bool? EsEcommerce { get; set; }
    public string? TipoEnvio { get; set; }
    public string? Distrito { get; set; }

    public Cliente? Cliente { get; set; }
    public TipoDocumentoVenta TipoDocumentoVenta { get; set; } = null!;
    public List<ComprobanteDetalle> ComprobanteDetalles { get; set; } = new List<ComprobanteDetalle>();
    public List<Pago> Pagos { get; set; } = new List<Pago>();
}


