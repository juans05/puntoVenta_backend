namespace Domain.Payloads;

public class CreateSucursalPayload
{
    public string Nombre { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? UbigeoId { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public int MonedaId { get; set; }
    public int PaisId { get; set; }
    public int RubroId { get; set; }
    public string? SerieFactura { get; set; }
    public string? SerieBoleta { get; set; }
    public int? EmpiezaEnFactura { get; set; }
    public int? EmpiezaEnBoleta { get; set; }
    public string? CodigoEstablecimiento { get; set; }
    public string? Urbanizacion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string TenantId { get; set; } = null!;
}