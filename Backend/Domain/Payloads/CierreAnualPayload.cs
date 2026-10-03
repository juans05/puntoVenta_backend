namespace Domain.Payloads;

public class CierreAnualPayload
{
    public int Anio { get; set; }
    // % de Impuesto a la Renta sobre el resultado antes de impuestos (cuenta 88).
    public decimal TasaImpuestoRenta { get; set; }
    // El sistema no calcula la reexpresion NIC 21: el usuario confirma que ya la ejecuto.
    public bool ConfirmaDifCambio { get; set; }
}
