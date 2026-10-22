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

    [HttpPost("probar-conexion")]
    public async Task<IActionResult> Probar()
        => await _repo.ProbarConexion() is { } error ? BadRequest(new { message = error }) : Ok(new { message = "Conexión con SUNAT correcta" });
}
