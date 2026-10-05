using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Infrastructure.Repositories;

// Sin interfaz/servicio a proposito (ponytail): son dos consultas y una escritura. Se extrae si
// el carrito/pedidos web lo vuelven necesario.
public class TiendaRepository
{
    private static readonly Regex Color = new("^#[0-9a-fA-F]{6}$");
    private static readonly Regex Telefono = new(@"^\+?\d{7,15}$");
    private readonly SpaContext _context;

    private static bool UrlValida(string? url)
        => string.IsNullOrWhiteSpace(url)
           || (url.Trim().Length <= 500 && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps));

    public TiendaRepository(SpaContext context) => _context = context;

    public async Task<TiendaConfig> ObtenerConfig()
        => await _context.TiendaConfig.AsNoTracking().FirstOrDefaultAsync() ?? new TiendaConfig();

    public async Task<string?> GuardarConfig(TiendaConfig c)
    {
        if (!Color.IsMatch(c.ColorPrimario) || !Color.IsMatch(c.ColorFondo) || !Color.IsMatch(c.ColorTexto))
            return "Los colores deben tener formato #RRGGBB";
        if (string.IsNullOrWhiteSpace(c.Titulo) || c.Titulo.Length > 100) return "El título es obligatorio (máx. 100 caracteres)";
        if (c.Descripcion?.Length > 300) return "La descripción admite máx. 300 caracteres";
        if (!UrlValida(c.LogoUrl) || !UrlValida(c.BannerUrl)) return "Logo y banner deben ser URLs http(s) de hasta 500 caracteres";
        if (!string.IsNullOrWhiteSpace(c.Whatsapp) && !Telefono.IsMatch(c.Whatsapp.Trim())) return "WhatsApp debe tener solo dígitos (con código de país)";

        var actual = await _context.TiendaConfig.AsTracking().FirstOrDefaultAsync();
        if (actual == null) _context.TiendaConfig.Add(actual = new TiendaConfig());
        actual.Publicada = c.Publicada;
        actual.Titulo = c.Titulo.Trim();
        actual.Descripcion = c.Descripcion?.Trim();
        actual.LogoUrl = c.LogoUrl?.Trim();
        actual.BannerUrl = c.BannerUrl?.Trim();
        actual.ColorPrimario = c.ColorPrimario;
        actual.ColorFondo = c.ColorFondo;
        actual.ColorTexto = c.ColorTexto;
        actual.Whatsapp = c.Whatsapp?.Trim();
        await _context.SaveChangesAsync();
        return null;
    }

    // Publico: el tenant viene de la ruta, no del JWT, asi que se filtra a mano (IgnoreQueryFilters)
    // en vez de depender del filtro por tenant/sucursal del contexto.
    public async Task<object?> ObtenerPublica(string tenant)
    {
        var buscado = tenant.Trim().ToLower();
        // El enlace del admin lleva el TenantKey (claim "empresa"), pero los datos se guardan con
        // TenantId = Tenant.Name: se resuelve por cualquiera de los dos.
        var t = await _context.Tenant.AsNoTracking()
            .Where(x => x.Activo && (x.TenantKey.ToLower() == buscado || x.Name.ToLower() == buscado))
            .Select(x => x.Name.ToLower()).FirstOrDefaultAsync();
        if (t == null) return null;
        var config = await _context.TiendaConfig.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId.ToLower() == t && c.Estado);
        if (config == null || !config.Publicada) return null;

        var productos = await _context.Producto.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.TenantId.ToLower() == t && p.Estado && p.SeVende)
            .OrderBy(p => p.Nombre).Take(500)
            .Select(p => new
            {
                p.Id, p.Nombre, p.Descripcion, p.Marca,
                imagen = p.RutaImagen,
                precio = p.PrecioVentaConInpuesto ?? p.Precio,
                categoria = p.Categoria != null ? p.Categoria.Nombre : null,
                agotado = !p.EsServicio && (p.Stock ?? 0) <= 0
            }).ToListAsync();

        return new
        {
            config = new { config.Titulo, config.Descripcion, config.LogoUrl, config.BannerUrl, config.ColorPrimario, config.ColorFondo, config.ColorTexto, config.Whatsapp },
            productos
        };
    }
}
