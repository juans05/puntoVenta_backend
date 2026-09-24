namespace Domain.Entities;

public class Compra : EntityBase
{
    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }
    public string NumeroCompra { get; set; } = null!;
    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public decimal Total { get; set; }
    public int? MetodoPagoId { get; set; }
    public Metodopago? Metodopago { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public DateTime FechaCompra { get; set; }
    public string? Observacion { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public int? MonedaId { get; set; }
    public Moneda? Moneda { get; set; }
    public int? TipoIgvId { get; set; }
    public TipoIgv? TipoIgv { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public decimal? MontoDescuento { get; set; }
    public decimal? OtrosCargos { get; set; }
    public bool EsCredito { get; set; }
    // Vencimiento de la compra a credito (cuentas por pagar / antiguedad). Null = usa FechaCompra.
    public DateTime? FechaVencimiento { get; set; }
    public decimal ValorGravada { get; set; }
    public decimal ValorIgv { get; set; }
    // Factura de una orden de compra (flujo completo): el stock ya subio en la recepcion.
    public int? OrdenCompraId { get; set; }
    public bool StockYaIngresado { get; set; }
    public List<CompraDetalle> CompraDetalles { get; set; } = new();
}