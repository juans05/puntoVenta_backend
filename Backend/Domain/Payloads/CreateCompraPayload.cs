namespace Domain.Payloads;

public class CreateCompraPayload
{
    public int? SucursalId { get; set; }
    public int? ProveedorId { get; set; }
    public int? MetodoPagoId { get; set; }
    public string? Observacion { get; set; }
    public DateTime? FechaCompra { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public DateTime? FechaEmision { get; set; }
    public int? MonedaId { get; set; }
    public int? TipoIgvId { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public decimal? MontoDescuento { get; set; }
    public decimal? OtrosCargos { get; set; }
    public bool EsCredito { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal? TipoCambio { get; set; }
    public int? TipoDetraccionId { get; set; }
    public string? NumeroDetraccion { get; set; }
    public DateTime? FechaDetraccion { get; set; }

    // Datos del proveedor cuando aun no existe en el catalogo local: si ProveedorId viene vacio
    // pero hay Ruc, se busca/crea por Ruc (mismo criterio que la importacion de XML).
    public string? ProveedorRuc { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorDireccion { get; set; }
    public string? ProveedorUbigeoId { get; set; }
    public string? ProveedorEmail { get; set; }

    // Solo lo honra CrearCompraDeOrden (flujo completo); CrearCompra lo ignora.
    public int? OrdenCompraId { get; set; }

    public List<CompraDetallePayload> Detalle { get; set; } = new();
}

public class CompraDetallePayload
{
    // Linea de bien: ProductoId del catalogo. Linea de servicio (viene de una Orden de Servicio):
    // ProductoId null + Descripcion libre. OrdenCompraDetalleId identifica la linea de origen
    // cuando la factura viene de una orden (FacturarOrden lo completa antes de llamar aqui).
    public int? ProductoId { get; set; }
    public string? Descripcion { get; set; }
    public int? OrdenCompraDetalleId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public int? CentroCostoId { get; set; }
    public int? CuentaContableId { get; set; }
}