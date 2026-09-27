namespace Domain.Payloads;

// Linea que un repositorio (OrdenCompra/Compra/Cuentas) pide generar en un asiento contable.
// No se expone por HTTP: la genera codigo interno, no el usuario.
public class LineaAsientoContable
{
    public string CuentaCodigo { get; set; } = null!;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }

    public LineaAsientoContable() { }
    public LineaAsientoContable(string cuentaCodigo, decimal debe, decimal haber)
    {
        CuentaCodigo = cuentaCodigo;
        Debe = debe;
        Haber = haber;
    }
}
