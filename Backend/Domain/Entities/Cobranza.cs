namespace Domain.Entities;

// Cobro a un cliente (SAP B1: Pago recibido). Puede cubrir varias facturas a credito, en forma parcial.
public class Cobranza : EntityBase
{
    public int? SucursalId { get; set; }
    public string Numero { get; set; } = null!;
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTime Fecha { get; set; }
    public int MetodoPagoId { get; set; }
    public Metodopago? Metodopago { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public string EstadoCobranza { get; set; } = EstadoPagoCuenta.Activo;
    public List<CobranzaDetalle> Detalles { get; set; } = new();
}

public class CobranzaDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int CobranzaId { get; set; }
    public Cobranza? Cobranza { get; set; }
    public int ComprobanteCabeceraId { get; set; }
    public ComprobanteCabecera? ComprobanteCabecera { get; set; }
    public decimal MontoAplicado { get; set; }
}

// Pago a un proveedor (SAP B1: Pago efectuado) sobre compras a credito.
public class PagoProveedor : EntityBase
{
    public int? SucursalId { get; set; }
    public string Numero { get; set; } = null!;
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public DateTime Fecha { get; set; }
    public int MetodoPagoId { get; set; }
    public Metodopago? Metodopago { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public string EstadoPago { get; set; } = EstadoPagoCuenta.Activo;
    public List<PagoProveedorDetalle> Detalles { get; set; } = new();
}

public class PagoProveedorDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int PagoProveedorId { get; set; }
    public PagoProveedor? PagoProveedor { get; set; }
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }
    public decimal MontoAplicado { get; set; }
}

public static class EstadoPagoCuenta
{
    public const string Activo = "ACTIVO";
    public const string Anulado = "ANULADO";
}
