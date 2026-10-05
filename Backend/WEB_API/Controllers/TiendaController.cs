using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[ApiController]
[Route("api/tienda")]
public class TiendaController : ControllerBase
{
    private readonly TiendaRepository _repo;

    public TiendaController(TiendaRepository repo) => _repo = repo;

    [AllowAnonymous]
    [HttpGet("publica/{tenant}")]
    public async Task<IActionResult> Publica(string tenant)
        => await _repo.ObtenerPublica(tenant) is { } data ? Ok(data) : NotFound("Tienda no disponible");

    [AllowAnonymous]
    [HttpGet("publica/{tenant}/producto/{id:int}")]
    public async Task<IActionResult> Producto(string tenant, int id)
        => await _repo.ObtenerProductoPublico(tenant, id) is { } data ? Ok(data) : NotFound("Producto no disponible");

    [Authorize(Policy = "TiendaAdmin")]
    [HttpGet("config")]
    public async Task<IActionResult> Config() => Ok(await _repo.ObtenerConfig());

    [Authorize(Policy = "TiendaAdmin")]
    [HttpPut("config")]
    public async Task<IActionResult> GuardarConfig([FromBody] TiendaConfig payload)
        => await _repo.GuardarConfig(payload) is { } error ? BadRequest(error) : Ok(await _repo.ObtenerConfig());
}
