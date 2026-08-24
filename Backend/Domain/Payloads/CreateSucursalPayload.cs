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
    public string TenantId { get; set; } = null!;
}