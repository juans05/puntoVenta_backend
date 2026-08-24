namespace Domain.Models;
public class SummaryRequest
{
    public string fecGeneracion { get; set; }
    public string fecResumen { get; set; }
    public string correlativo { get; set; }
    public string moneda { get; set; }
    public Company company { get; set; }
    public List<Detaile> details { get; set; }
}

public class Detaile
{
    public string tipoDoc { get; set; }
    public string serieNro { get; set; }
    public string estado { get; set; }
    public string clienteTipo { get; set; }
    public string clienteNro { get; set; }
    public decimal total { get; set; }
    public decimal mtoOperGravadas { get; set; }
    public decimal mtoOperInafectas { get; set; }
    public decimal mtoOperExoneradas { get; set; }
    public decimal mtoOperExportacion { get; set; }
    public decimal mtoOtrosCargos { get; set; }
    public decimal mtoIGV { get; set; }
}




