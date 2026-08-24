using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/cash")]
[ApiController]
public class CierreDiarioController : ControllerBase
{
    private readonly ICierreDiarioService _cierreDiarioService;

    public CierreDiarioController(ICierreDiarioService cierreDiarioService)
    {
        _cierreDiarioService = cierreDiarioService;
    }

    [HttpGet("resumen-dia")]
    public async Task<IActionResult> ResumenDia() => Ok(await _cierreDiarioService.ResumenDia());

    [HttpPost("close")]
    public async Task<IActionResult> CerrarDia([FromBody] CierreDiarioPayload payload) => Ok(await _cierreDiarioService.CerrarDia(payload));

    [HttpGet("cierres")]
    public async Task<IActionResult> ListarCierres([FromQuery] CierreDiarioQueryParams payload) => Ok(await _cierreDiarioService.ListarCierres(payload));
}