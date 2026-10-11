using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace API.Middlewares;

// Security headers en toda respuesta + limite de intentos por IP en las rutas publicas sensibles
// (login de staff y de clientes, formulario publico de pedidos, webhook). .NET 6 no trae
// AddRateLimiter, por eso es una ventana fija en memoria.
public class SeguridadMiddleware
{
    private readonly RequestDelegate _next;

    // (prefijo de ruta, intentos permitidos por minuto). Solo se cuentan los POST.
    private static readonly (string Ruta, int Limite)[] Limites =
    {
        ("/api/autenticacion/token", 10),
        ("/api/cliente/auth", 10),
        ("/api/pedidos/publico", 20),
        ("/api/whatsapp/webhook", 60),
    };

    // ponytail: contador en memoria por instancia; con varias replicas usar Redis o un rate limiter del proxy.
    private static readonly ConcurrentDictionary<string, (DateTime Inicio, int Conteo)> Ventanas = new();
    private static DateTime _ultimaLimpieza = DateTime.UtcNow;

    public SeguridadMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        // HSTS solo si la peticion llego por HTTPS (detras de Railway el TLS lo termina el proxy
        // y avisa con X-Forwarded-Proto; UseHsts no lo ve porque internamente es HTTP).
        if (context.Request.IsHttps || context.Request.Headers["X-Forwarded-Proto"] == "https")
            headers["Strict-Transport-Security"] = "max-age=31536000";
        // La API solo devuelve JSON/archivos: nada de scripts ni iframes. Swagger (solo en
        // Development) necesita sus propios scripts, por eso no se le aplica.
        if (context.Request.Path.StartsWithSegments("/api"))
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        if (HttpMethods.IsPost(context.Request.Method) && Excedido(context))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new { message = "Demasiados intentos. Espera un minuto e inténtalo de nuevo." });
            return;
        }

        await _next(context);
    }

    private static bool Excedido(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var regla = Limites.FirstOrDefault(l => path.StartsWith(l.Ruta, StringComparison.OrdinalIgnoreCase));
        if (regla.Ruta is null) return false;

        var ahora = DateTime.UtcNow;
        if (ahora - _ultimaLimpieza > TimeSpan.FromMinutes(5))
        {
            _ultimaLimpieza = ahora;
            foreach (var k in Ventanas.Where(v => ahora - v.Value.Inicio > TimeSpan.FromMinutes(1)).Select(v => v.Key).ToList())
                Ventanas.TryRemove(k, out _);
        }

        var clave = $"{regla.Ruta}|{IpCliente(context)}";
        var ventana = Ventanas.AddOrUpdate(clave, _ => (ahora, 1),
            (_, v) => ahora - v.Inicio > TimeSpan.FromMinutes(1) ? (ahora, 1) : (v.Inicio, v.Conteo + 1));
        return ventana.Conteo > regla.Limite;
    }

    // Detras del proxy (Railway) la IP real llega en X-Forwarded-For; se toma el ULTIMO valor,
    // que es el que agrega el proxy (los anteriores los puede inventar el cliente).
    private static string IpCliente(HttpContext context)
    {
        var xff = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xff))
            return xff.Split(',').Last().Trim();
        return context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    }
}
