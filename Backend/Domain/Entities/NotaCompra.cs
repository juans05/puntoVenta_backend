namespace Domain.Entities;

// Nota de credito/debito RECIBIDA del proveedor sobre una Compra ya registrada (descuento,
// devolucion parcial/total, cargo adicional). Espejo de ComprobanteRepository.CrearNotaCreditoDebito
// (ventas) pero del lado compras: siempre afecta el monto completo de la Compra, igual que aquel.
// A diferencia de AnularCompra (cancela todo el documento), una NotaCompra es su propio movimiento
// contable y no cambia el Estado de la Compra original.
public class NotaCompra : EntityBase
{
    public string Numero { get; set; } = null!;
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }
    public string Tipo { get; set; } = null!;
    public int MotivoNotaId { get; set; }
    public MotivoNota? MotivoNota { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string EstadoNota { get; set; } = EstadoNotaCompra.Activa;
    public string? Observacion { get; set; }
}

public static class TipoNotaCompra
{
    public const string Credito = "CREDITO";
    public const string Debito = "DEBITO";
}

public static class EstadoNotaCompra
{
    public const string Activa = "ACTIVA";
    public const string Anulada = "ANULADA";
}
