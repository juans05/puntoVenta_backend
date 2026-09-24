namespace Domain.DTO;

public class CompraDto
{
    public int Id { get; set; }
    public string NumeroCompra { get; set; } = null!;
    public int? SucursalId { get; set; }
    public string? Sucursal { get; set; }
    public int? ProveedorId { get; set; }
    public string? Proveedor { get; set; }
    public decimal Total { get; set; }
    public int? MetodoPagoId { get; set; }
    public string? MetodoPago { get; set; }
    public string Estado { get; set; } = null!;
    public string FechaRegistro { get; set; } = null!;
    public string FechaCompra { get; set; } = null!;
    public string? Observacion { get; set; }
    public string? Usuario { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public string? FechaEmision { get; set; }
    public int? MonedaId { get; set; }
    public string? Moneda { get; set; }
    public int? TipoIgvId { get; set; }
    public string? TipoIgv { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public decimal? MontoDescuento { get; set; }
    public decimal? OtrosCargos { get; set; }
    public bool EsCredito { get; set; }
    public string? FechaVencimiento { get; set; }
    public decimal ValorGravada { get; set; }
    public decimal ValorIgv { get; set; }
    public List<CompraDetalleDto> Detalle { get; set; } = new();
}

public class CompraDetalleDto
{
    public int ProductoId { get; set; }
    public string? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}