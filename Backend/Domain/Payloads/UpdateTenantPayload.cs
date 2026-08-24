namespace Domain.Payloads;

public class UpdateTenantPayload
{
    public int Id { get; set; }
    public string? Ruc { get; set; }
    public string? NombreComercial { get; set; }
    public string? RazonSocial { get; set; }
    public string? Direccion { get; set; }
    public string? Celular { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? SitioWeb { get; set; }
    public string? ImagenPortada { get; set; }
    public string? GifCarga { get; set; }
    public string? LogoSidebar { get; set; }
    public string? Logo { get; set; }
    public string? UbigeoId { get; set; }
}
