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
    private async Task<(string Tenant, TiendaConfig Config)?> ResolverTienda(string tenant)
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
        return config == null || !config.Publicada ? null : (t, config);
    }

    private static object ConfigPublica(TiendaConfig c)
        => new { c.Titulo, c.Descripcion, c.LogoUrl, c.BannerUrl, c.ColorPrimario, c.ColorFondo, c.ColorTexto, c.Whatsapp };

    // Solo lo que se vende y esta disponible (servicio o con stock).
    private IQueryable<Producto> Vendibles(string tenant)
        => _context.Producto.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.TenantId.ToLower() == tenant && p.Estado && p.SeVende && (p.EsServicio || (p.Stock ?? 0) > 0));

    // Nunca se exponen costo, margen, precio minimo, cuentas contables ni proveedor.
    private static IQueryable<object> Publico(IQueryable<Producto> q) => q.Select(p => (object)new
    {
        p.Id, p.Nombre, p.Descripcion, p.Marca,
        imagen = p.RutaImagen,
        p.VideoUrl,
        p.Galeria,
        precio = p.PrecioVentaConInpuesto ?? p.Precio,
        categoria = p.Categoria != null ? p.Categoria.Nombre : null,
        grupo = p.Grupo != null ? p.Grupo.Nombre : null,
        codigo = p.Codigo,
        p.CodigoBarra,
        unidadMedida = p.UnidadMedida != null ? p.UnidadMedida.Descripcion : null,
        pesoKg = p.PesoKg,
        tipoIgv = p.TipoIgv != null ? p.TipoIgv.Descripcion : null,
        p.Icbper,
        p.RestriccionEdad,
        p.EsServicio,
        stock = p.EsServicio ? (int?)null : p.Stock,
        presentaciones = p.Presentaciones.Where(x => x.Estado)
            .Select(x => new { x.Nombre, unidad = x.UnidadMedida != null ? x.UnidadMedida.Descripcion : null, x.Factor, x.PrecioVenta }).ToList(),
        agotado = !p.EsServicio && (p.Stock ?? 0) <= 0
    });

    public async Task<object?> ObtenerPublica(string tenant)
    {
        if (await ResolverTienda(tenant) is not var (t, config)) return null;
        var productos = await Publico(Vendibles(t).OrderBy(p => p.Nombre).Take(500)).ToListAsync();
        return new { config = ConfigPublica(config), productos };
    }

    // Landing de un producto: el producto y hasta 4 de la misma categoria. Mismo criterio de
    // disponibilidad que el catalogo, asi que un producto agotado/oculto da 404.
    public async Task<object?> ObtenerProductoPublico(string tenant, int id)
    {
        if (await ResolverTienda(tenant) is not var (t, config)) return null;
        var actual = await Vendibles(t).Where(p => p.Id == id).Select(p => new { p.CategoriaId }).FirstOrDefaultAsync();
        if (actual == null) return null;

        var producto = await Publico(Vendibles(t).Where(p => p.Id == id)).FirstAsync();
        var relacionados = await Publico(Vendibles(t)
            .Where(p => p.Id != id && (actual.CategoriaId == null || p.CategoriaId == actual.CategoriaId))
            .OrderBy(p => p.Nombre).Take(4)).ToListAsync();
        return new { config = ConfigPublica(config), producto, relacionados };
    }
}
