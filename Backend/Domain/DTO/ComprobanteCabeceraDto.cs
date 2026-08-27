namespace Domain.DTO;


public class ComprobanteCabeceraDTO
{
    public int Index { get; set; }
    public int IdComprobante { get; set; }
    public int? ClienteId { get; set; }
    public string ClienteNombre { get; set; }

    public int TipoDocumentoVentaId { get; set; }
    public string TipoDocumentoVenta { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorSubtotal { get; set; }
    public decimal ValorIgv { get; set; }
    public string Serie { get; set; } = null!;
    public string Correlativo { get; set; }
    public string NombreVendedor { get; set; }
    public string Fecha { get; set; }
    public string FechaVenta { get; set; }
    public string EstadoComprobante { get; set; }
    public string EstadoEnvioSunat { get; set; }
    public bool? EsEcommerce { get; set; }
    public string? TipoEnvio { get; set; }
    public string? Distrito { get; set; }

    public List<ComprobanteDetalleDTO> ComprobanteDetalles { get; set; } = new List<ComprobanteDetalleDTO>();
    public List<PagoDTO> Pagos { get; set; } = new List<PagoDTO>();
}

public class ComprobanteDetalleDTO
{
    public int ProductoId { get; set; }
    public string Producto { get; set; }
    public string RutaImagen { get; set; }
    public int Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorUnitarioTotal { get; set; }
    public decimal ValorIgv { get; set; }
}

public class PagoDTO
{
    public int ComprobanteCabeceraId { get; set; }
    public int MetodoPagoId { get; set; }
    public string MetodoPago { get; set; }
    public decimal Monto { get; set; }
}