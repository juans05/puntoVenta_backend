using Application.Services;
using Domain.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[ApiController]
[Route("api/pedidos-venta")]
public class PedidoVentaController : ControllerBase
{
    private readonly PedidoVentaService _service;

    public PedidoVentaController(PedidoVentaService service)
    {
        _service = service;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> Crear([FromBody] CreatePedidoVentaPayload payload) => Ok(await _service.Crear(payload));

    [HttpPost("desde-cotizacion/{cotizacionId}")]
    public async Task<IActionResult> DesdeCotizacion(int cotizacionId) => Ok(await _service.DesdeCotizacion(cotizacionId));

    [HttpPut("actualizar/{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CreatePedidoVentaPayload payload) => Ok(await _service.Actualizar(id, payload));

    [HttpGet("listar")]
    public async Task<IActionResult> Listar([FromQuery] PedidoVentaQueryParams payload) => Ok(await _service.Listar(payload));

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(int id) => Ok(await _service.Obtener(id));

    [HttpPut("{id}/confirmar")]
    public async Task<IActionResult> Confirmar(int id) => Ok(await _service.Confirmar(id));

    [HttpPut("{id}/anular")]
    public async Task<IActionResult> Anular(int id) => Ok(await _service.Anular(id));

    [HttpPut("{id}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] CerrarPedidoVentaPayload payload) => Ok(await _service.Cerrar(id, payload.Motivo));

    [HttpPost("{id}/entregas")]
    public async Task<IActionResult> Entregar(int id, [FromBody] CreateEntregaPayload payload) => Ok(await _service.Entregar(id, payload));

    [HttpPut("entregas/{entregaId}/anular")]
    public async Task<IActionResult> AnularEntrega(int entregaId) => Ok(await _service.AnularEntrega(entregaId));
}
