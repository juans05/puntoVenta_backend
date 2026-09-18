namespace Domain.Models;
public class NoteRequest
{
    public string ublVersion { get; set; }
    public string tipoDoc { get; set; } // "07" Nota de Credito / "08" Nota de Debito
    public string tipoDocAfectado { get; set; }
    public string numDocfectado { get; set; }
    public string codMotivo { get; set; }
    public string desMotivo { get; set; }
    public string serie { get; set; }
    public string correlativo { get; set; }
    public string fechaEmision { get; set; }
    public FormaPago formaPago { get; set; }
    public string tipoMoneda { get; set; }
    public Client client { get; set; }
    public Company company { get; set; }
    public decimal mtoOperGravadas { get; set; }
    public decimal mtoIGV { get; set; }
    public decimal valorVenta { get; set; }
    public decimal totalImpuestos { get; set; }
    public decimal subTotal { get; set; }
    public decimal mtoImpVenta { get; set; }
    public List<Detail> details { get; set; }
    public List<Legend> legends { get; set; }
}
