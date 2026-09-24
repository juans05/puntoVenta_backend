namespace Domain.DTO;

public class ConfiguracionFlujoDto
{
    public string FlujoCompras { get; set; } = null!;
    public string CruceFactura { get; set; } = null!;
    public decimal? MontoAprobacionOc { get; set; }
    public string FlujoVentas { get; set; } = null!;
}

public class OrdenCompraDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public int? SucursalId { get; set; }
    public string? Sucursal { get; set; }
    public int? ProveedorId { get; set; }
    public string? Proveedor { get; set; }
    public int? MonedaId { get; set; }
    public string FechaEmision { get; set; } = null!;
    public decimal Total { get; set; }
    public string EstadoOrden { get; set; } = null!;
    public string? Observacion { get; set; }
    public string? AprobadoPor { get; set; }
    public string? MotivoCierre { get; set; }
    public string? Usuario { get; set; }
    public List<OrdenCompraDetalleDto> Detalle { get; set; } = new();
    public List<RecepcionDto> Recepciones { get; set; } = new();
}

public class OrdenCompraDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? Producto { get; set; }
    public int CantidadPedida { get; set; }
    public int CantidadRecibida { get; set; }
    public int CantidadFacturada { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class RecepcionDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public string Fecha { get; set; } = null!;
    public string EstadoRecepcion { get; set; } = null!;
    public string? Observacion { get; set; }
}
