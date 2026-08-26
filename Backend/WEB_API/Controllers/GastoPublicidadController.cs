using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/gastopublicidad")]
[ApiController]
public class GastoPublicidadController : ControllerBase
{
    private readonly IGastoPublicidadService _gastoPublicidadService;

    public GastoPublicidadController(IGastoPublicidadService gastoPublicidadService)
    {
        _gastoPublicidadService = gastoPublicidadService;
    }

    [HttpPost("importar")]
    public async Task<IActionResult> Importar([FromBody] ImportarGastoPublicidadPayload payload) => Ok(await _gastoPublicidadService.Importar(payload));

    [HttpGet("roi")]
    public async Task<IActionResult> CalcularRoi([FromQuery] GastoPublicidadRoiQueryParams payload) => Ok(await _gastoPublicidadService.CalcularRoi(payload));

    [HttpGet("listar")]
    public async Task<IActionResult> Listar([FromQuery] GastoPublicidadQueryParams payload) => Ok(await _gastoPublicidadService.Listar(payload));

    [HttpPost("mapeos-anuncios")]
    public async Task<IActionResult> ObtenerMapeosAnuncios([FromBody] ObtenerMapeosAnunciosPayload payload) => Ok(await _gastoPublicidadService.ObtenerMapeosAnuncios(payload));
}
