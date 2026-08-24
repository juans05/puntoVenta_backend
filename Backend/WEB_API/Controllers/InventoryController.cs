using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/inventario")]
[ApiController]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> ListarMovimientos([FromQuery] InventoryMovementQuery payload) => Ok(await _inventoryService.ListarMovimientos(payload));

    [HttpPost("ajustar-stock")]
    public async Task<IActionResult> AjustarStock([FromBody] CreateAjusteInventarioPayload payload) => Ok(await _inventoryService.AjustarStock(payload));
}