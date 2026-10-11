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
    public string? ProveedorRuc { get; set; }
    public string? ProveedorDireccion { get; set; }
    public int? MonedaId { get; set; }
    public string FechaEmision { get; set; } = null!;
    public decimal Total { get; set; }
    public string EstadoOrden { get; set; } = null!;
    public string TipoOrden { get; set; } = null!;
    public string? Observacion { get; set; }
    public string? AprobadoPor { get; set; }
    public string? MotivoCierre { get; set; }
    public string? Usuario { get; set; }
    public int? DepartamentoId { get; set; }
    public string? Departamento { get; set; }
    public string? AprobadorAsignadoId { get; set; }
    public string? AprobadorAsignado { get; set; }
    public List<OrdenCompraDetalleDto> Detalle { get; set; } = new();
    public List<RecepcionDto> Recepciones { get; set; } = new();
}

public class OrdenCompraDetalleDto
{
    public int Id { get; set; }
    public int? ProductoId { get; set; }
    public string? Producto { get; set; }
    public string? Descripcion { get; set; }
    public int CantidadPedida { get; set; }
    public int CantidadRecibida { get; set; }
    public int CantidadFacturada { get; set; }
    public decimal CostoUnitario { get; set; }
    public int? CentroCostoId { get; set; }
    public string? CentroCosto { get; set; }
    public int? CuentaContableId { get; set; }
    public string? CuentaContable { get; set; }
}

public class RecepcionDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public string Fecha { get; set; } = null!;
    public string EstadoRecepcion { get; set; } = null!;
    public string? Observacion { get; set; }
    public string? NumeroGuiaRemision { get; set; }
}
