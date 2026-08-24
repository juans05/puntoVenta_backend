using System.Text.Json.Serialization;

namespace Domain.Models;


public class InvoiceResponse
{
    public string xml { get; set; }
    public string hash { get; set; }
    public SunatResponse sunatResponse { get; set; }
}

public class SunatResponse
{
    public bool success { get; set; }
    public string cdrZip { get; set; }
    public string ticket { get; set; }//solo para anulaciones viene esta propiedad
    public Error error { get; set; }
    public CdrResponse cdrResponse { get; set; }
}

public class Error
{
    public string code { get; set; }
    public string message { get; set; }
}

public class CdrResponse
{
    public string id { get; set; }
    public string code { get; set; }
    public string description { get; set; }
    public List<object> notes { get; set; }
}






