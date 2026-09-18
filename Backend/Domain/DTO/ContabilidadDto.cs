namespace Domain.DTO;

// Registro de Ventas e Ingresos (PLE 14.1) -- una fila por comprobante fiscal (Factura/Boleta/NC/ND).
public class LibroVentaDto
{
    public string Periodo { get; set; } = null!;
    public string Cuo { get; set; } = null!;
    public string FechaEmision { get; set; } = null!;
    public string? FechaVencimiento { get; set; }
    public string TipoComprobante { get; set; } = null!;
    public string Serie { get; set; } = null!;
    public string Numero { get; set; } = null!;
    public string TipoDocCliente { get; set; } = null!;
    public string? NumeroDocCliente { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BaseImponibleGravada { get; set; }
    public decimal DescuentoBaseImponible { get; set; }
    public decimal OpExonerada { get; set; }
    public decimal OpInafecta { get; set; }
    public decimal Igv { get; set; }
    public decimal ImporteTotal { get; set; }
    public string Moneda { get; set; } = null!;
    public decimal? TipoCambio { get; set; }
    public string? FechaDocModificado { get; set; }
    public string? TipoDocModificado { get; set; }
    public string? SerieDocModificado { get; set; }
    public string? NumeroDocModificado { get; set; }
    public string Estado { get; set; } = null!;
}

// Registro de Compras (PLE 8.1) -- una fila por compra registrada.
public class LibroCompraDto
{
    public string Periodo { get; set; } = null!;
    public string Cuo { get; set; } = null!;
    public string FechaEmision { get; set; } = null!;
    public string TipoComprobante { get; set; } = null!;
    public string Serie { get; set; } = null!;
    public string Numero { get; set; } = null!;
    public string TipoDocProveedor { get; set; } = null!;
    public string? NumeroDocProveedor { get; set; }
    public string? RazonSocial { get; set; }
    public decimal BaseImponibleGravada { get; set; }
    public decimal ValorAdquisicionesNoGravadas { get; set; }
    public decimal Igv { get; set; }
    public decimal ImporteTotal { get; set; }
    public string Moneda { get; set; } = null!;
    public string Estado { get; set; } = null!;
}

// Reporte detallado: una fila por producto dentro de cada documento (no por documento).
public class ReporteDetalladoVentaDto
{
    public string Fecha { get; set; } = null!;
    public string SerieCorrelativo { get; set; } = null!;
    public string TipoComprobante { get; set; } = null!;
    public string? Cliente { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Igv { get; set; }
    public decimal Importe { get; set; }
}

public class ReporteDetalladoCompraDto
{
    public string Fecha { get; set; } = null!;
    public string SerieNumero { get; set; } = null!;
    public string? Proveedor { get; set; }
    public string? Ruc { get; set; }
    public string? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
