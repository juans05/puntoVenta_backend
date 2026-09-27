using Domain.Entities.Identity;

namespace Domain.Entities;

public class OrdenCompra : EntityBase
{
    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }
    public string Numero { get; set; } = null!;
    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public int? MonedaId { get; set; }
    public Moneda? Moneda { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal Total { get; set; }
    public string EstadoOrden { get; set; } = EstadoOrdenCompra.Borrador;
    // Bien: pasa por Recepcion antes de facturar. Servicio: no hay bien fisico, no hay
    // InventoryMovement -- al emitirse queda "recibida" de una vez (ver EmitirOrden/CrearOrden).
    public string TipoOrden { get; set; } = TipoOrdenCompra.Bien;
    public string? Observacion { get; set; }
    public string? AprobadoPor { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? MotivoCierre { get; set; }
    // Solo llenos cuando la orden necesita aprobacion (total > ConfiguracionFlujo.MontoAprobacionOc):
    // el departamento elegido y el UNICO aprobador de ese departamento asignado a esta orden.
    public int? DepartamentoId { get; set; }
    public Departamento? Departamento { get; set; }
    public string? AprobadorAsignadoId { get; set; }
    public User? AprobadorAsignado { get; set; }
    public List<OrdenCompraDetalle> Detalles { get; set; } = new();
    public List<Recepcion> Recepciones { get; set; } = new();
}

public class OrdenCompraDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int OrdenCompraId { get; set; }
    public OrdenCompra? OrdenCompra { get; set; }
    // Linea de bien: ProductoId del catalogo. Linea de servicio: null + Descripcion libre.
    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string? Descripcion { get; set; }
    public int CantidadPedida { get; set; }
    public int CantidadRecibida { get; set; }
    public int CantidadFacturada { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class Recepcion : EntityBase
{
    public int? SucursalId { get; set; }
    public string Numero { get; set; } = null!;
    public int OrdenCompraId { get; set; }
    public OrdenCompra? OrdenCompra { get; set; }
    public DateTime Fecha { get; set; }
    public string EstadoRecepcion { get; set; } = "ACTIVA";
    public string? Observacion { get; set; }
    public List<RecepcionDetalle> Detalles { get; set; } = new();
}

public class RecepcionDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int RecepcionId { get; set; }
    public Recepcion? Recepcion { get; set; }
    public int OrdenCompraDetalleId { get; set; }
    public OrdenCompraDetalle? OrdenCompraDetalle { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}

public static class TipoOrdenCompra
{
    public const string Bien = "BIEN";
    public const string Servicio = "SERVICIO";
}

public static class EstadoOrdenCompra
{
    public const string Borrador = "BORRADOR";
    public const string PendienteAprobacion = "PENDIENTE_APROBACION";
    public const string Emitida = "EMITIDA";
    public const string RecibidaParcial = "RECIBIDA_PARCIAL";
    public const string Recibida = "RECIBIDA";
    public const string Cerrada = "CERRADA";
    public const string Anulada = "ANULADA";
}
