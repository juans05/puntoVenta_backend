using System.Net.Http.Json;
using System.Security.Claims;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SunatCredencialesPayload
{
    public string? UsuarioSol { get; set; }
    // Vacios = se conservan los guardados (el front nunca recibe los secretos).
    public string? ClaveSol { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

// Credenciales de la empresa activa para la API de SUNAT (SIRE) y obtencion del token OAuth con
// Clave SOL. Solo el dueño de la empresa las ve y las cambia: dan acceso a todo el RUC ante SUNAT.
public class SunatRepository
{
    private const string ScopeSire = "https://api-sire.sunat.gob.pe";

    private readonly SpaContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;

    public SunatRepository(SpaContext context, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
    }

    private async Task<bool> EsPropietario()
    {
        var user = _httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("nameid");
        return await _context.UsuarioEmpresa.AnyAsync(x =>
            x.UserId == userId && x.EsPropietario && x.Estado && x.Tenant.Name == _context.CurrentTenantName);
    }

    private Task<Domain.Entities.ConfiguracionFiscal?> Configuracion()
        => _context.ConfiguracionFiscal.IgnoreQueryFilters()
            .Where(x => x.TenantId == _context.CurrentTenantName && x.Activo && x.Estado)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

    public async Task<(object? Data, string? Error)> Obtener()
    {
        if (!await EsPropietario()) return (null, "Solo el dueño de la empresa puede ver las credenciales de SUNAT");
        var c = await Configuracion();
        return (new
        {
            ruc = c?.Ruc,
            usuarioSol = c?.UsuarioSol,
            clientId = c?.SunatClientId,
            tieneClaveSol = !string.IsNullOrEmpty(c?.ClaveSol),
            tieneClientSecret = !string.IsNullOrEmpty(c?.SunatClientSecret)
        }, null);
    }

    public async Task<string?> Guardar(SunatCredencialesPayload payload)
    {
        if (!await EsPropietario()) return "Solo el dueño de la empresa puede cambiar las credenciales de SUNAT";
        var c = await Configuracion();
        if (c is null) return "La empresa no tiene configuración fiscal";

        c.UsuarioSol = payload.UsuarioSol?.Trim().ToUpper();
        c.SunatClientId = payload.ClientId?.Trim();
        if (!string.IsNullOrWhiteSpace(payload.ClaveSol)) c.ClaveSol = payload.ClaveSol;
        if (!string.IsNullOrWhiteSpace(payload.ClientSecret)) c.SunatClientSecret = payload.ClientSecret.Trim();

        await _context.SaveChangesRegularAsync();
        return null;
    }

    // Token de la API SIRE (dura ~1 hora). Error = mensaje listo para mostrar.
    // detalleCredenciales: solo desde ProbarConexion (exclusivo del dueño) se muestra con que usuario SOL se intento;
    // "Traer de SUNAT" lo usan tambien los contadores y no deben ver el usuario SOL.
    public async Task<(string? Token, string? Error)> ObtenerTokenSire(bool detalleCredenciales = false)
    {
        var c = await Configuracion();
        if (c is null || string.IsNullOrWhiteSpace(c.Ruc))
            return (null, "Registra el RUC de la empresa en Datos de la empresa");
        if (string.IsNullOrWhiteSpace(c.UsuarioSol) || string.IsNullOrEmpty(c.ClaveSol)
            || string.IsNullOrWhiteSpace(c.SunatClientId) || string.IsNullOrEmpty(c.SunatClientSecret))
            return (null, "Completa usuario SOL, clave SOL, client ID y client secret");

        // SUNAT espera RUC + usuario SOL en mayusculas y sin espacios; la web de SOL lo tolera, la API no.
        var ruc = c.Ruc.Trim();
        var usuarioSol = c.UsuarioSol.Trim().ToUpperInvariant();
        var cliente = _httpClientFactory.CreateClient();
        cliente.Timeout = TimeSpan.FromSeconds(30);
        try
        {
            var respuesta = await cliente.PostAsync(
                $"https://api-seguridad.sunat.gob.pe/v1/clientessol/{Uri.EscapeDataString(c.SunatClientId)}/oauth2/token/",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["scope"] = ScopeSire,
                    ["client_id"] = c.SunatClientId,
                    ["client_secret"] = c.SunatClientSecret,
                    ["username"] = ruc + usuarioSol,
                    ["password"] = c.ClaveSol
                }));

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, object?>>() ?? new();
            if (respuesta.IsSuccessStatusCode && cuerpo.TryGetValue("access_token", out var token) && token is not null)
                return (token.ToString(), null);

            var detalle = cuerpo.TryGetValue("error_description", out var d) ? d?.ToString()
                        : cuerpo.TryGetValue("error", out var e) ? e?.ToString() : null;
            var error = $"SUNAT rechazó las credenciales{(string.IsNullOrEmpty(detalle) ? "" : $": {detalle}")}";
            return (null, detalleCredenciales
                ? $"{error}. Se intentó con el RUC {ruc} y el usuario SOL {usuarioSol}: verifica que ese usuario pertenezca a ese RUC, la clave SOL y el client ID/secret de ese mismo RUC."
                : $"{error}. El dueño de la empresa puede revisarlas en Mi Empresa → Certificado y Clave SOL.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return (null, "No se pudo conectar con SUNAT, intenta de nuevo en unos minutos");
        }
    }

    // Ademas de pedir el token hace una consulta real a SIRE (periodos del Registro de Ventas): SUNAT puede
    // entregar el token y aun asi rechazar las consultas a SIRE.
    public async Task<string?> ProbarConexion()
    {
        if (!await EsPropietario()) return "Solo el dueño de la empresa puede probar las credenciales de SUNAT";
        var (token, error) = await ObtenerTokenSire(detalleCredenciales: true);
        if (token is null) return error;

        var cliente = ClienteSire(token);
        try
        {
            var r = await cliente.GetAsync($"{BaseSire}/rvierce/padron/web/omisos/{LibroRvie}/periodos");
            if (r.IsSuccessStatusCode) return null;
            return r.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? await MensajeSireNoAutorizado(r, token)
                : $"SUNAT respondió HTTP {(int)r.StatusCode} al consultar SIRE: {await Recorte(r)}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return "No se pudo conectar con SUNAT, intenta de nuevo en unos minutos";
        }
    }

    private HttpClient ClienteSire(string token)
    {
        var cliente = _httpClientFactory.CreateClient();
        cliente.Timeout = TimeSpan.FromSeconds(60);
        cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        cliente.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return cliente;
    }

    // El 401 de SIRE suele venir con el motivo en WWW-Authenticate o en el cuerpo: se muestra tal cual.
    private static async Task<string> MensajeSireNoAutorizado(HttpResponseMessage r, string token)
    {
        var motivo = string.Join(" ", r.Headers.WwwAuthenticate.Select(h => h.ToString()));
        return "SUNAT entregó el token pero rechazó la consulta a SIRE (HTTP 401). " +
               $"Motivo: {(string.IsNullOrWhiteSpace(motivo) ? "no indicado" : motivo)}. Respuesta: {await Recorte(r)}. " +
               $"Datos del token: {DatosDelToken(token)}";
    }

    // ---------------------------------------------------------------- Ventas (RVIE)
    // Manual "API del Registro de Ventas e Informacion Electronica" v30: 5.18 descargar propuesta
    // (genera un ticket), 5.16 consultar estado del ticket y 5.17 descargar archivo (zip con el txt).

    private const string BaseSire = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros";
    private const string LibroRvie = "140000";

    // Propuesta de ventas del periodo (yyyymm) cruzada con lo emitido en el sistema. SUNAT la arma
    // en segundo plano: si aun no esta lista se devuelve el ticket para volver a consultar con el.
    public async Task<(object? Data, string? Error)> PropuestaVentas(string periodo, string? numTicket)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(periodo ?? "", @"^\d{6}$"))
            return (null, "El periodo debe tener el formato AAAAMM");
        // El ticket viaja en la URL hacia SUNAT: solo se aceptan digitos (formato AAAA99999999).
        if (!string.IsNullOrEmpty(numTicket) && !System.Text.RegularExpressions.Regex.IsMatch(numTicket, @"^\d{1,20}$"))
            return (null, "Ticket inválido");

        var (token, errorToken) = await ObtenerTokenSire();
        if (token is null) return (null, errorToken);

        var cliente = ClienteSire(token);

        try
        {
            if (string.IsNullOrWhiteSpace(numTicket))
            {
                var r = await cliente.GetAsync($"{BaseSire}/rvie/propuesta/web/propuesta/{periodo}/exportapropuesta?codTipoArchivo=0");
                var json = await LeerJson(r);
                if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (null, await MensajeSireNoAutorizado(r, token));
                if (!r.IsSuccessStatusCode)
                    return (null, ErrorSunat(json, $"SUNAT no generó la propuesta (HTTP {(int)r.StatusCode}): {await Recorte(r)}"));
                numTicket = json?["numTicket"]?.ToString();
                if (string.IsNullOrEmpty(numTicket)) return (null, "SUNAT no devolvió el ticket de la propuesta");
            }

            // El archivo suele estar listo en segundos; se espera un poco antes de devolver el ticket.
            for (var intento = 0; intento < 8; intento++)
            {
                if (intento > 0) await Task.Delay(TimeSpan.FromSeconds(3));
                var archivo = await ArchivoDelTicket(cliente, periodo, numTicket);
                if (archivo is null) continue;

                var contenido = await cliente.GetAsync(
                    $"{BaseSire}/rvierce/gestionprocesosmasivos/web/masivo/archivoreporte" +
                    $"?nomArchivoReporte={Uri.EscapeDataString(archivo.Value.Nombre)}&codTipoArchivoReporte={archivo.Value.Tipo}" +
                    $"&codLibro={LibroRvie}&perTributario={periodo}&codProceso={archivo.Value.Proceso}&numTicket={numTicket}");
                if (!contenido.IsSuccessStatusCode)
                    return (null, ErrorSunat(await LeerJson(contenido), "SUNAT no entregó el archivo de la propuesta"));

                var filas = LeerPropuesta(await contenido.Content.ReadAsByteArrayAsync());
                if (filas is null) return (null, "El archivo de SUNAT no tiene el formato esperado");
                return (new { estado = "LISTO", numTicket, comprobantes = await CruzarConSistema(filas) }, null);
            }

            return (new { estado = "PROCESANDO", numTicket }, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return (null, "No se pudo conectar con SUNAT, intenta de nuevo en unos minutos");
        }
    }

    // Archivo generado para el ticket, o null si SUNAT aun lo esta procesando.
    private async Task<(string Nombre, string Tipo, string Proceso)?> ArchivoDelTicket(HttpClient cliente, string periodo, string numTicket)
    {
        var r = await cliente.GetAsync(
            $"{BaseSire}/rvierce/gestionprocesosmasivos/web/masivo/consultaestadotickets" +
            $"?perIni={periodo}&perFin={periodo}&page=1&perPage=20&numTicket={numTicket}&codLibro={LibroRvie}&codOrigenEnvio=2");
        if (!r.IsSuccessStatusCode) return null;

        var registros = (await LeerJson(r))?["registros"]?.AsArray();
        var registro = registros?.FirstOrDefault(x => x?["numTicket"]?.ToString() == numTicket) ?? registros?.FirstOrDefault();
        var archivo = registro?["archivoReporte"]?.AsArray().FirstOrDefault();
        var nombre = archivo?["nomArchivoReporte"]?.ToString();
        if (string.IsNullOrEmpty(nombre)) return null;

        // El manual escribe "codTipoAchivoReporte" (sin r) en la salida; se aceptan ambas formas.
        var tipo = (archivo!["codTipoArchivoReporte"] ?? archivo["codTipoAchivoReporte"])?.ToString() ?? "null";
        return (nombre, tipo, registro!["codProceso"]?.ToString() ?? "");
    }

    private static async Task<System.Text.Json.Nodes.JsonNode?> LeerJson(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        try { return string.IsNullOrWhiteSpace(texto) ? null : System.Text.Json.Nodes.JsonNode.Parse(texto); }
        catch (System.Text.Json.JsonException) { return null; }
    }

    // Claims del JWT de SUNAT utiles para diagnosticar permisos (sin firma ni el token en si).
    private static string DatosDelToken(string token)
    {
        try
        {
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = System.Text.Json.Nodes.JsonNode.Parse(Convert.FromBase64String(payload))!;
            var datos = json["userdata"];
            var campos = new[]
            {
                ("scope", json["scope"]?.ToString()),
                ("aud", json["aud"]?.ToString()),
                ("tipoUsuario", datos?["tipUsuario"]?.ToString()),
            };
            var texto = string.Join(", ", campos.Where(c => !string.IsNullOrEmpty(c.Item2)).Select(c => $"{c.Item1}={c.Item2}"));
            return string.IsNullOrEmpty(texto) ? "sin datos" : texto;
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or System.Text.Json.JsonException)
        {
            return "no se pudo leer";
        }
    }

    // Respuesta cruda de SUNAT (recortada) para cuando no viene en el formato de error documentado.
    private static async Task<string> Recorte(HttpResponseMessage r)
    {
        var texto = (await r.Content.ReadAsStringAsync()).Trim();
        return string.IsNullOrEmpty(texto) ? "sin detalle" : texto.Length > 300 ? texto[..300] + "..." : texto;
    }

    // SUNAT responde {cod, msg, errors:[{cod, msg}]}: se muestra el primer error concreto.
    private static string ErrorSunat(System.Text.Json.Nodes.JsonNode? json, string porDefecto)
    {
        var detalle = json?["errors"]?.AsArray().FirstOrDefault()?["msg"]?.ToString() ?? json?["msg"]?.ToString();
        return string.IsNullOrEmpty(detalle) ? porDefecto : $"SUNAT: {detalle}";
    }

    public record ComprobanteSunat(string Fecha, string Tipo, string Serie, string Numero, string? DocCliente,
        string? Cliente, decimal BaseGravada, decimal Igv, decimal Total, string? Moneda);

    // El txt (RS 112-2021, separado por "|") trae una fila de encabezados: las columnas se ubican por
    // nombre y no por posicion, para no romperse si SUNAT agrega o reordena columnas.
    public static List<ComprobanteSunat>? LeerPropuesta(byte[] archivo)
    {
        string texto;
        if (archivo.Length > 1 && archivo[0] == 'P' && archivo[1] == 'K')
        {
            using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(archivo));
            var entrada = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) ?? zip.Entries.FirstOrDefault();
            if (entrada is null) return null;
            using var lector = new StreamReader(entrada.Open(), System.Text.Encoding.UTF8);
            texto = lector.ReadToEnd();
        }
        else texto = System.Text.Encoding.UTF8.GetString(archivo);

        var lineas = texto.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Trim().Length > 0).ToList();
        if (lineas.Count == 0) return new();

        var cab = lineas[0].Split('|').Select(Normalizar).ToList();
        int Col(Func<string, bool> criterio) => cab.FindIndex(c => criterio(c));
        var iFecha = Col(c => c.Contains("fecha") && c.Contains("emision"));
        var iTipo = Col(c => c.StartsWith("tipo cp"));
        var iSerie = Col(c => c.StartsWith("serie"));
        var iNumero = Col(c => c.StartsWith("nro cp"));
        var iDoc = Col(c => c.StartsWith("nro doc identidad"));
        var iCliente = cab.FindLastIndex(c => c.Contains("razon social"));
        var iBase = Col(c => c.StartsWith("bi gravada"));
        var iIgv = Col(c => c.StartsWith("igv"));
        var iTotal = Col(c => c.StartsWith("total cp"));
        var iMoneda = Col(c => c == "moneda");
        if (iSerie < 0 || iNumero < 0 || iTotal < 0) return null;

        string? Val(string[] f, int i) => i >= 0 && i < f.Length ? f[i].Trim() : null;
        decimal Num(string[] f, int i) => decimal.TryParse(Val(f, i), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : 0;

        return lineas.Skip(1).Select(l => l.Split('|')).Select(f => new ComprobanteSunat(
            Val(f, iFecha) ?? "", Val(f, iTipo) ?? "", Val(f, iSerie) ?? "", Val(f, iNumero) ?? "",
            Val(f, iDoc), Val(f, iCliente), Num(f, iBase), Num(f, iIgv), Num(f, iTotal), Val(f, iMoneda))).ToList();
    }

    private static string Normalizar(string s)
    {
        var sinTildes = new string(s.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        return sinTildes.Trim().ToLowerInvariant();
    }

    // Marca cada comprobante de SUNAT segun exista en el sistema (misma serie y numero, cualquier sede).
    private async Task<object> CruzarConSistema(List<ComprobanteSunat> filas)
    {
        var series = filas.Select(f => f.Serie).Distinct().ToList();
        var locales = await _context.ComprobanteCabecera.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.TenantId == _context.CurrentTenantName && series.Contains(c.Serie))
            .Select(c => new { c.Serie, c.Correlativo, c.EstadoComprobante })
            .ToListAsync();
        var estadoLocal = locales.GroupBy(c => (c.Serie, c.Correlativo)).ToDictionary(g => g.Key, g => g.First().EstadoComprobante);

        return filas.Select(f =>
        {
            var existe = int.TryParse(f.Numero, out var n) && estadoLocal.TryGetValue((f.Serie, n), out _);
            return new
            {
                f.Fecha, f.Tipo, f.Serie, f.Numero, f.DocCliente, f.Cliente, f.BaseGravada, f.Igv, f.Total, f.Moneda,
                enSistema = existe,
                anuladoEnSistema = existe && estadoLocal[(f.Serie, n)] == Domain.Enumerations.EstatusComprobante.Anulado
            };
        }).ToList();
    }
}
