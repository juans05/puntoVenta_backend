namespace Domain.Payloads;

// Registro publico desde el login: datos del dueño y de su primera empresa.
public class RegistroPayload
{
    public string Nombres { get; set; } = null!;
    public string? Apellidos { get; set; }
    public string Email { get; set; } = null!;
    public string? Celular { get; set; }
    public string Usuario { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public string? NombreComercial { get; set; }
    public string? Ruc { get; set; }
    public int RubroId { get; set; }
}
