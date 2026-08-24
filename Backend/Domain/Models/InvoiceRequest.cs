namespace Domain.Models;
public class InvoiceRequest
{
    public string ublVersion { get; set; }
    public string tipoOperacion { get; set; }
    public string tipoDoc { get; set; }
    public string serie { get; set; }
    public string correlativo { get; set; }
    public string fechaEmision { get; set; }//podria ser datetime
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

public class Address
{
    public string direccion { get; set; }
    public string provincia { get; set; }
    public string departamento { get; set; }
    public string distrito { get; set; }
    public string ubigueo { get; set; }
}

public class Client
{
    public string tipoDoc { get; set; }
    public string numDoc { get; set; }
    public string rznSocial { get; set; }
    public Address address { get; set; }
}

public class Company
{
    public long ruc { get; set; }
    public string razonSocial { get; set; }
    public string nombreComercial { get; set; }
    public Address address { get; set; }
}

public class Detail
{
    public string codProducto { get; set; }
    public string unidad { get; set; }
    public string descripcion { get; set; }
    public int cantidad { get; set; }
    public decimal mtoValorUnitario { get; set; }
    public decimal mtoValorVenta { get; set; }
    public decimal mtoBaseIgv { get; set; }
    public decimal porcentajeIgv { get; set; }
    public decimal igv { get; set; }
    public string tipAfeIgv { get; set; }
    public decimal totalImpuestos { get; set; }
    public decimal mtoPrecioUnitario { get; set; }
}

public class FormaPago
{
    public string moneda { get; set; }
    public string tipo { get; set; }
}

public class Legend
{
    public string code { get; set; }
    public string value { get; set; }
}



