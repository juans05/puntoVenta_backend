using Application.Interfaces.IServices;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[Route("api/whatsapp")]
[ApiController]
[AllowAnonymous]
public class WhatsappController : ControllerBase
{
    private readonly IWhatsappService _whatsappService;

    public WhatsappController(IWhatsappService whatsappService)
    {
        _whatsappService = whatsappService;
    }

    [HttpGet("webhook")]
    public IActionResult Verify(string hub_mode, string hub_challenge, string hub_verify_token)
    {
        if (hub_mode == "subscribe" && hub_verify_token == "spa_webhook_token")
            return Ok(hub_challenge);

        return Unauthorized();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> RecibirMensaje([FromBody] WhatsappMessagePayload payload)
    {
        var resultado = await _whatsappService.ProcesarMensaje(payload);
        return Ok(new { resultado.Intencion, resultado.Respuesta, resultado.RequiereConfirmacion, resultado.PayloadJson });
    }
}