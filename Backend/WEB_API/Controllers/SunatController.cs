using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

// Credenciales de la API de SUNAT (Clave SOL + credenciales de API) de la empresa activa.
[ApiController]
[Route("api/sunat")]
public class SunatController : ControllerBase
{
    private readonly SunatRepository _repo;

    public SunatController(SunatRepository repo) => _repo = repo;

    [HttpGet("credenciales")]
    public async Task<IActionResult> Obtener()
    {
        var (data, error) = await _repo.Obtener();
        return error is null ? Ok(new { data }) : BadRequest(new { message = error });
    }

    [HttpPut("credenciales")]
    public async Task<IActionResult> Guardar([FromBody] SunatCredencialesPayload payload)
        => await _repo.Guardar(payload) is { } error ? BadRequest(new { message = error }) : Ok(new { message = "Credenciales guardadas" });

    // Ventas registradas en SUNAT (propuesta RVIE) del periodo AAAAMM, cruzadas con el sistema.
    // Si SUNAT aun esta generando el archivo devuelve estado=PROCESANDO y el ticket para reintentar.
    [HttpGet("ventas/{periodo}")]
    public async Task<IActionResult> Ventas(string periodo, [FromQuery] string? numTicket)
    {
        var (data, error) = await _repo.PropuestaVentas(periodo, numTicket);
        return error is null ? Ok(new { data }) : BadRequest(new { message = error });
    }

    // Registra en el sistema los comprobantes elegidos (serie-numero) de la propuesta ya consultada.
    [HttpPost("ventas/{periodo}/importar")]
    public async Task<IActionResult> Importar(string periodo, [FromBody] ImportarVentasSunatPayload payload)
    {
        var (importados, omitidos, error) = await _repo.ImportarVentas(periodo, payload.NumTicket, payload.Comprobantes);
        return error is null ? Ok(new { data = new { importados, omitidos } }) : BadRequest(new { message = error });
    }

    // Facturas de compra que los proveedores emitieron al RUC (propuesta RCE) del periodo AAAAMM,
    // marcando las que ya estan registradas como compra. Mismo esquema de ticket que ventas.
    [HttpGet("compras/{periodo}")]
    public async Task<IActionResult> Compras(string periodo, [FromQuery] string? numTicket)
    {
        var (data, error) = await _repo.PropuestaCompras(periodo, numTicket);
        return error is null ? Ok(new { data }) : BadRequest(new { message = error });
    }

    [HttpPost("probar-conexion")]
    public async Task<IActionResult> Probar()
        => await _repo.ProbarConexion() is { } error ? BadRequest(new { message = error }) : Ok(new { message = "Conexión con SUNAT correcta" });
}
