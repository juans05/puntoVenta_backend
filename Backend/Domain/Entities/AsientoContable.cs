namespace Domain.Entities;

// Origen del asiento: que operacion del ERP lo generó (para poder ubicarlo/reversarlo).
public static class OrigenAsientoContable
{
    public const string MovimientoInventario = "MovimientoInventario";
    public const string Factura = "Factura";
    public const string Pago = "Pago";
    public const string Venta = "Venta";
    public const string Cobro = "Cobro";
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
}
