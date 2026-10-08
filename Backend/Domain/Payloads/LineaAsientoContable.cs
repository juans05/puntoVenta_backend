namespace Domain.Payloads;

// Linea que un repositorio (OrdenCompra/Compra/Cuentas) pide generar en un asiento contable.
// Tambien la usa el asiento manual (AsientoContableController.Crear).
public class LineaAsientoContable
{
    public string CuentaCodigo { get; set; } = null!;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public string? CuentaAsociada { get; set; }
    public int? CentroCosto1Id { get; set; }
    public int? CentroCosto2Id { get; set; }
    public int? CuentaDestinoId { get; set; }
    public string? Descripcion { get; set; }

    public LineaAsientoContable() { }
    public LineaAsientoContable(string cuentaCodigo, decimal debe, decimal haber)
    {
        CuentaCodigo = cuentaCodigo;
        Debe = debe;
        Haber = haber;
    }
}
