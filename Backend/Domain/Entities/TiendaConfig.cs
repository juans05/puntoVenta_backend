namespace Domain.Entities;

// Tienda web publica del tenant (una fila por tenant): tema editable y textos. Los productos que
// muestra salen del catalogo (Producto con SeVende y Estado activos), no se duplican aqui.
public class TiendaConfig : EntityBase
{
    public bool Publicada { get; set; }
    public string Titulo { get; set; } = "Mi tienda";
    public string? Descripcion { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string ColorPrimario { get; set; } = "#4f46e5";
    public string ColorFondo { get; set; } = "#ffffff";
    public string ColorTexto { get; set; } = "#111827";
    public string? Whatsapp { get; set; }
}
