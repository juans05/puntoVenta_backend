namespace Domain.Payloads;

public class ConfiguracionFlujoPayload
{
    public string FlujoCompras { get; set; } = null!;
    public string CruceFactura { get; set; } = null!;
    public decimal? MontoAprobacionOc { get; set; }
}

public class OrdenCompraQueryParams
{
    public int Page { get; set; } = 1;
    public int Amount { get; set; } = 50;
    public string? Estado { get; set; }
}

public class CreateOrdenCompraPayload
{
    public int? SucursalId { get; set; }
    public int? ProveedorId { get; set; }
    public int? MonedaId { get; set; }
    public string? Observacion { get; set; }
    // true = queda en BORRADOR; false = se emite (o queda pendiente de aprobacion segun el monto).
    public bool Borrador { get; set; }
    public List<OrdenCompraDetallePayload> Detalle { get; set; } = new();
}

public class OrdenCompraDetallePayload
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class CerrarOrdenCompraPayload
{
    public string? Motivo { get; set; }
}

public class CreateRecepcionPayload
{
    public string? Observacion { get; set; }
    public List<RecepcionDetallePayload> Detalle { get; set; } = new();
}

public class RecepcionDetallePayload
{
    public int OrdenCompraDetalleId { get; set; }
    public int Cantidad { get; set; }
}

// Factura del proveedor de una orden. Reutiliza los campos de la compra; el detalle va por producto.
public class FacturarOrdenCompraPayload : CreateCompraPayload
{
    public bool ConfirmarDiferencias { get; set; }
}
