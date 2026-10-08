namespace Domain.Entities;

// Origen del asiento: que operacion del ERP lo generó (para poder ubicarlo/reversarlo).
public static class OrigenAsientoContable
{
    public const string MovimientoInventario = "MovimientoInventario";
    public const string Factura = "Factura";
    public const string Pago = "Pago";
    public const string Venta = "Venta";
    public const string Cobro = "Cobro";
    public const string NotaCompra = "NotaCompra";
    // Metodo PCGE 60/61: la entrada/salida de mercaderia va en su propio asiento, separado de la factura.
    public const string EntradaCompra = "EntradaCompra";         // 20 / 61 de una compra directa (OrigenId = Compra.Id)
    public const string NotaCompraEntrada = "NotaCompraEntrada"; // reverso de EntradaCompra por nota de credito (OrigenId = NotaCompra.Id)
    public const string SalidaVenta = "SalidaVenta";             // 69 / 20 de una venta (OrigenId = ComprobanteCabecera.Id)
    public const string SalidaEntrega = "SalidaEntrega";         // 69 / 20 de una entrega de pedido (OrigenId = Entrega.Id)
    public const string Manual = "Manual";                       // registrado a mano (OrigenId = su propio Id)
    public const string AjusteInventario = "AjusteInventario";   // 20 <-> 61 de un ajuste de stock (OrigenId = InventoryMovement.Id)
}

public static class EstadoAsientoContable
{
    public const string Emitido = "EMITIDO";
    public const string Anulado = "ANULADO";
}

public class AsientoContable : EntityBase
{
    public string Numero { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public string Glosa { get; set; } = null!;
    public string OrigenTipo { get; set; } = null!;
    public int OrigenId { get; set; }
    public string EstadoAsiento { get; set; } = EstadoAsientoContable.Emitido;
    // Fecha = fecha de contabilizacion. Estas dos vienen del documento origen (factura, etc.), si aplica.
    public DateTime? FechaDocumento { get; set; }
    public DateTime? FechaVencimiento { get; set; }

    public List<AsientoContableDetalle> Detalle { get; set; } = new();
}

public class AsientoContableDetalle : EntityBase
{
    public int AsientoContableId { get; set; }
    public AsientoContable? AsientoContable { get; set; }

    public int CuentaContableId { get; set; }
    public CuentaContable? CuentaContable { get; set; }

    public decimal Debe { get; set; }
    public decimal Haber { get; set; }

    // Tercero de la linea (RUC/DNI del proveedor o cliente), texto libre como en el libro diario.
    public string? CuentaAsociada { get; set; }
    // Dos ejes analiticos del mismo catalogo CentroCosto (ej. area y proyecto).
    public int? CentroCosto1Id { get; set; }
    public CentroCosto? CentroCosto1 { get; set; }
    public int? CentroCosto2Id { get; set; }
    public CentroCosto? CentroCosto2 { get; set; }
    // Cuenta 9X elegida a mano para el destino de un gasto 62-68 (sustituye la configuracion de la cuenta).
    public int? CuentaDestinoId { get; set; }
    public CuentaContable? CuentaDestino { get; set; }
    public string? Descripcion { get; set; }
}
