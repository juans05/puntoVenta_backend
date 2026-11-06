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

public class ImportarVentasSunatPayload
{
    public string NumTicket { get; set; } = "";
    public List<string> Comprobantes { get; set; } = new();
}

// Credenciales de la empresa activa para la API de SUNAT (SIRE) y obtencion del token OAuth con
// Clave SOL. Solo el dueño de la empresa las ve y las cambia: dan acceso a todo el RUC ante SUNAT.
public class SunatRepository
{
    private const string ScopeSire = "https://api-sire.sunat.gob.pe";

    private readonly SpaContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Application.Interfaces.IRepository.IAsientoContableRepository _asientos;

    public SunatRepository(SpaContext context, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory,
        Application.Interfaces.IRepository.IAsientoContableRepository asientos)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _asientos = asientos;
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
            var r = await GetSire(cliente, $"{BaseSire}/rvierce/padron/web/omisos/{LibroRvie}/periodos");
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

    // El manual de SIRE pide Content-Type: application/json tambien en los GET; HttpClient solo lo envia con contenido.
    private static Task<HttpResponseMessage> GetSire(HttpClient cliente, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url) { Content = new ByteArrayContent(Array.Empty<byte>()) };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return cliente.SendAsync(request);
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
    private const string LibroRce = "080000";

    // Propuesta de ventas del periodo (yyyymm) cruzada con lo emitido en el sistema. SUNAT la arma
    // en segundo plano: si aun no esta lista se devuelve el ticket para volver a consultar con el.
    public async Task<(object? Data, string? Error)> PropuestaVentas(string periodo, string? numTicket)
    {
        var (filas, ticket, error) = await ObtenerPropuesta(periodo, numTicket, LibroRvie);
        if (error is not null) return (null, error);
        return filas is null
            ? (new { estado = "PROCESANDO", numTicket = ticket }, null)
            : (new { estado = "LISTO", numTicket = ticket, comprobantes = await CruzarConSistema(filas) }, null);
    }

    // ---------------------------------------------------------------- Compras (RCE)
    // Manual "API del Registro de Compras Electronico": misma mecanica de ticket/archivo que ventas,
    // con el libro 080000. La propuesta trae las facturas que los proveedores emitieron al RUC
    // (cabecera y montos, sin productos): el front las abre una por una en el formulario de compra.
    public async Task<(object? Data, string? Error)> PropuestaCompras(string periodo, string? numTicket)
    {
        var (filas, ticket, error) = await ObtenerPropuesta(periodo, numTicket, LibroRce);
        if (error is not null) return (null, error);
        return filas is null
            ? (new { estado = "PROCESANDO", numTicket = ticket }, null)
            : (new { estado = "LISTO", numTicket = ticket, comprobantes = await CruzarConCompras(filas) }, null);
    }

    // Una compra ya registrada se reconoce por RUC del proveedor + serie + numero (sin ceros a la izquierda:
    // SUNAT manda "123" y el usuario pudo escribir "00000123").
    private async Task<object> CruzarConCompras(List<ComprobanteSunat> filas)
    {
        static string Num(string? n) => (n ?? "").Trim().TrimStart('0');
        var series = filas.Select(f => f.Serie).Distinct().ToList();
        var locales = await _context.Compra.AsNoTracking()
            .Where(c => c.Estado != "ANULADO" && c.Serie != null && series.Contains(c.Serie))
            .Select(c => new { Ruc = c.Proveedor != null ? c.Proveedor.Ruc : null, c.Serie, c.Numero })
            .ToListAsync();
        var registradas = locales.Select(c => $"{c.Ruc}|{c.Serie}|{Num(c.Numero)}").ToHashSet();

        return filas.Select(f => new
        {
            f.Fecha, f.FechaVencimiento, f.Tipo, f.Serie, f.Numero, docProveedor = f.DocCliente, proveedor = f.Cliente,
            f.BaseGravada, f.Igv, f.Total, f.Moneda,
            enSistema = registradas.Contains($"{f.DocCliente}|{f.Serie}|{Num(f.Numero)}")
        }).ToList();
    }

    // Filas de la propuesta, o filas=null (con el ticket) si SUNAT aun no termina de armarla.
    private async Task<(List<ComprobanteSunat>? Filas, string? NumTicket, string? Error)> ObtenerPropuesta(string periodo, string? numTicket, string libro)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(periodo ?? "", @"^\d{6}$"))
            return (null, null, "El periodo debe tener el formato AAAAMM");
        // El ticket viaja en la URL hacia SUNAT: solo se aceptan digitos (formato AAAA99999999).
        if (!string.IsNullOrEmpty(numTicket) && !System.Text.RegularExpressions.Regex.IsMatch(numTicket, @"^\d{1,20}$"))
            return (null, null, "Ticket inválido");

        var (token, errorToken) = await ObtenerTokenSire();
        if (token is null) return (null, null, errorToken);

        var cliente = ClienteSire(token);

        try
        {
            if (string.IsNullOrWhiteSpace(numTicket))
            {
                var r = await GetSire(cliente, libro == LibroRce
                    ? $"{BaseSire}/rce/propuesta/web/propuesta/{periodo}/exportacioncomprobantepropuesta?codTipoArchivo=0&codOrigenEnvio=2"
                    : $"{BaseSire}/rvie/propuesta/web/propuesta/{periodo}/exportapropuesta?codTipoArchivo=0");
                var json = await LeerJson(r);
                if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (null, null, await MensajeSireNoAutorizado(r, token));
                if (!r.IsSuccessStatusCode)
                    return (null, null, ErrorSunat(json, $"SUNAT no generó la propuesta (HTTP {(int)r.StatusCode}): {await Recorte(r)}"));
                numTicket = json?["numTicket"]?.ToString();
                if (string.IsNullOrEmpty(numTicket)) return (null, null, "SUNAT no devolvió el ticket de la propuesta");
            }

            // El archivo suele estar listo en segundos; se espera un poco antes de devolver el ticket.
            for (var intento = 0; intento < 8; intento++)
            {
                if (intento > 0) await Task.Delay(TimeSpan.FromSeconds(3));
                var archivo = await ArchivoDelTicket(cliente, periodo, numTicket, libro);
                if (archivo is null) continue;

                var contenido = await GetSire(cliente, 
                    $"{BaseSire}/rvierce/gestionprocesosmasivos/web/masivo/archivoreporte" +
                    $"?nomArchivoReporte={Uri.EscapeDataString(archivo.Value.Nombre)}&codTipoArchivoReporte={archivo.Value.Tipo}" +
                    $"&codLibro={libro}&perTributario={periodo}&codProceso={archivo.Value.Proceso}&numTicket={numTicket}");
                if (!contenido.IsSuccessStatusCode)
                    return (null, null, ErrorSunat(await LeerJson(contenido), "SUNAT no entregó el archivo de la propuesta"));

                var filas = LeerPropuesta(await contenido.Content.ReadAsByteArrayAsync());
                return filas is null ? (null, null, "El archivo de SUNAT no tiene el formato esperado") : (filas, numTicket, null);
            }

            return (null, numTicket, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return (null, null, "No se pudo conectar con SUNAT, intenta de nuevo en unos minutos");
        }
    }

    // Archivo generado para el ticket, o null si SUNAT aun lo esta procesando.
    private async Task<(string Nombre, string Tipo, string Proceso)?> ArchivoDelTicket(HttpClient cliente, string periodo, string numTicket, string libro)
    {
        var r = await GetSire(cliente, 
            $"{BaseSire}/rvierce/gestionprocesosmasivos/web/masivo/consultaestadotickets" +
            $"?perIni={periodo}&perFin={periodo}&page=1&perPage=20&numTicket={numTicket}&codLibro={libro}&codOrigenEnvio=2");
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
        string? Cliente, decimal BaseGravada, decimal Igv, decimal Total, string? Moneda, string? FechaVencimiento = null);

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
        // Ventas: "BI Gravada"; Compras: "BI Gravado DG" (destinada a operaciones gravadas).
        var iBase = Col(c => c.StartsWith("bi gravad"));
        var iVcto = Col(c => c.Contains("vcto"));
        var iIgv = Col(c => c.StartsWith("igv"));
        var iTotal = Col(c => c.StartsWith("total cp"));
        var iMoneda = Col(c => c == "moneda");
        if (iSerie < 0 || iNumero < 0 || iTotal < 0) return null;

        string? Val(string[] f, int i) => i >= 0 && i < f.Length ? f[i].Trim() : null;
        decimal Num(string[] f, int i) => decimal.TryParse(Val(f, i), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : 0;

        return lineas.Skip(1).Select(l => l.Split('|')).Select(f => new ComprobanteSunat(
            Val(f, iFecha) ?? "", Val(f, iTipo) ?? "", Val(f, iSerie) ?? "", Val(f, iNumero) ?? "",
            Val(f, iDoc), Val(f, iCliente), Num(f, iBase), Num(f, iIgv), Num(f, iTotal), Val(f, iMoneda), Val(f, iVcto))).ToList();
    }

    private static string Normalizar(string s)
    {
        var sinTildes = new string(s.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        return sinTildes.Trim().ToLowerInvariant();
    }

    // Importa los comprobantes elegidos (serie-numero) del periodo. Los montos, fechas y clientes se toman
    // del archivo de SUNAT, nunca del navegador: asi nadie puede registrar como "aceptada" una venta inventada.
    public async Task<(int Importados, List<string> Omitidos, string? Error)> ImportarVentas(string periodo, string numTicket, List<string> claves)
    {
        if (string.IsNullOrWhiteSpace(numTicket)) return (0, new(), "Vuelve a consultar el periodo antes de importar");
        var (filas, _, error) = await ObtenerPropuesta(periodo, numTicket, LibroRvie);
        if (error is not null) return (0, new(), error);
        if (filas is null) return (0, new(), "SUNAT aún está preparando el archivo, intenta de nuevo en unos segundos");
        var elegidas = new HashSet<string>(claves ?? new());
        var (importados, omitidos) = await RegistrarVentas(filas.Where(f => elegidas.Contains($"{f.Serie}-{f.Numero}")).ToList());
        return (importados, omitidos, null);
    }

    // Registra en el sistema facturas/boletas que solo estan en SUNAT (emitidas en otro sistema o en SEE-SOL).
    // La propuesta no trae productos: la venta queda sin detalle, ya aceptada (no pasa por el job de envio),
    // al credito y con su asiento 12 / 40 / 70. Devuelve cuantas se importaron y el motivo de las omitidas.
    public async Task<(int Importados, List<string> Omitidos)> RegistrarVentas(List<ComprobanteSunat> filas)
    {
        var omitidos = new List<string>();
        var importados = 0;
        foreach (var f in filas)
        {
            var nombre = $"{f.Serie}-{f.Numero}";
            int? tipo = f.Tipo.TrimStart('0') switch { "1" => (int)Domain.Enumerations.TipoComprobante.Factura, "3" => (int)Domain.Enumerations.TipoComprobante.Boleta, _ => null };
            if (tipo is null) { omitidos.Add($"{nombre}: solo se importan facturas y boletas"); continue; }
            if (!string.IsNullOrEmpty(f.Moneda) && f.Moneda != "PEN") { omitidos.Add($"{nombre}: solo se importan comprobantes en soles"); continue; }
            if (!int.TryParse(f.Numero, out var correlativo)
                || !DateTime.TryParseExact(f.Fecha, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fecha))
            { omitidos.Add($"{nombre}: número o fecha no válidos"); continue; }
            if (await _context.ComprobanteCabecera.IgnoreQueryFilters()
                    .AnyAsync(c => c.TenantId == _context.CurrentTenantName && c.Serie == f.Serie && c.Correlativo == correlativo))
            { omitidos.Add($"{nombre}: ya está en el sistema"); continue; }

            await _context.Database.BeginTransactionAsync();
            try
            {
                var cliente = string.IsNullOrWhiteSpace(f.DocCliente) ? null
                    : await _context.Cliente.FirstOrDefaultAsync(c => c.NumeroDocumento == f.DocCliente);
                if (cliente is null && !string.IsNullOrWhiteSpace(f.DocCliente) && !string.IsNullOrWhiteSpace(f.Cliente))
                {
                    cliente = new Domain.Entities.Cliente
                    {
                        NumeroDocumento = f.DocCliente,
                        Nombre = f.Cliente,
                        TipoDocumentoId = f.DocCliente.Length == 11 ? 5 : 1 // RUC : DNI (ver tipodocumento.json)
                    };
                    _context.Cliente.Add(cliente);
                    await _context.SaveChangesAsync();
                }

                var cabecera = new Domain.Entities.ComprobanteCabecera
                {
                    TipoDocumentoVentaId = tipo.Value,
                    ClienteId = cliente?.Id,
                    NumeroDocumento = f.DocCliente,
                    RazonSocial = f.Cliente,
                    Serie = f.Serie,
                    Correlativo = correlativo,
                    ValorTotal = f.Total,
                    ValorIgv = f.Igv,
                    ValorSubtotal = f.Total - f.Igv,
                    PorcentajeImpuesto = f.Total - f.Igv > 0 ? Math.Round(f.Igv * 100 / (f.Total - f.Igv), 2) : 0,
                    FechaVenta = fecha,
                    EsCredito = true,
                    EstadoComprobante = Domain.Enumerations.EstatusComprobante.Facturado,
                    EnviadoSunat = Domain.Enumerations.EstatusEnvioSunat.Enviado,
                    MensajeSunat = "Importado del Registro de Ventas de SUNAT (SIRE)",
                    Observacion = "Importado de SUNAT: sin detalle de productos"
                };
                _context.ComprobanteCabecera.Add(cabecera);
                await _context.SaveChangesAsync();

                var lineas = new List<Domain.Payloads.LineaAsientoContable>
                {
                    new("12", f.Total, 0) { CuentaAsociada = f.DocCliente },
                    new("70", 0, f.Total - f.Igv)
                };
                if (f.Igv > 0) lineas.Add(new("40111", 0, f.Igv));
                var (estado, _, error) = await _asientos.Generar(Domain.Entities.OrigenAsientoContable.Venta, cabecera.Id,
                    $"Venta {cabecera.Serie}-{cabecera.Correlativo}", lineas, fechaDocumento: fecha);
                if (estado != Domain.Models.ServiceStatus.Ok)
                {
                    await _context.Database.RollbackTransactionAsync();
                    _context.ChangeTracker.Clear();
                    omitidos.Add($"{nombre}: {error}");
                    continue;
                }
                await _context.Database.CommitTransactionAsync();
                importados++;
            }
            catch (DbUpdateException)
            {
                await _context.Database.RollbackTransactionAsync();
                _context.ChangeTracker.Clear();
                omitidos.Add($"{nombre}: no se pudo registrar");
            }
        }
        return (importados, omitidos);
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
