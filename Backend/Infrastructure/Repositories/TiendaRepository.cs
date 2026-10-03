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
    private readonly SpaContext _context;

    public TiendaRepository(SpaContext context) => _context = context;

    public async Task<TiendaConfig> ObtenerConfig()
        => await _context.TiendaConfig.AsNoTracking().FirstOrDefaultAsync() ?? new TiendaConfig();

    public async Task<string?> GuardarConfig(TiendaConfig c)
    {
        if (!Color.IsMatch(c.ColorPrimario) || !Color.IsMatch(c.ColorFondo) || !Color.IsMatch(c.ColorTexto))
            return "Los colores deben tener formato #RRGGBB";
        if (string.IsNullOrWhiteSpace(c.Titulo)) return "El título es obligatorio";

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
        var t = tenant.Trim().ToLower();
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
