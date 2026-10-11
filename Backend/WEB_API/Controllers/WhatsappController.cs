using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.IServices;
using Domain.Payloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

// El webhook no usa JWT (lo llama el integrador de WhatsApp, ej. n8n), asi que se protege con
// secretos compartidos en variables de entorno. Sin configurarlos, el webhook queda cerrado.
[Route("api/whatsapp")]
[ApiController]
[AllowAnonymous]
public class WhatsappController : ControllerBase
{
    private readonly IWhatsappService _whatsappService;
    private readonly IConfiguration _configuration;

    public WhatsappController(IWhatsappService whatsappService, IConfiguration configuration)
    {
        _whatsappService = whatsappService;
        _configuration = configuration;
    }

    [HttpGet("webhook")]
    public IActionResult Verify(string hub_mode, string hub_challenge, string hub_verify_token)
    {
        if (hub_mode == "subscribe" && Iguales(hub_verify_token, _configuration["WHATSAPP_VERIFY_TOKEN"]))
            return Ok(hub_challenge);

        return Unauthorized();
    }

    // El payload trae Username/SucursalId (actua en nombre de un usuario), por eso exige el
    // header X-Webhook-Secret igual a WHATSAPP_WEBHOOK_SECRET.
    [HttpPost("webhook")]
    public async Task<IActionResult> RecibirMensaje([FromBody] WhatsappMessagePayload payload)
    {
        if (!Iguales(Request.Headers["X-Webhook-Secret"], _configuration["WHATSAPP_WEBHOOK_SECRET"]))
            return Unauthorized();

        var resultado = await _whatsappService.ProcesarMensaje(payload);
        return Ok(new { resultado.Intencion, resultado.Respuesta, resultado.RequiereConfirmacion, resultado.PayloadJson });
    }

    // Comparacion en tiempo constante; si el secreto no esta configurado nunca coincide.
    private static bool Iguales(string? recibido, string? esperado) =>
        !string.IsNullOrEmpty(esperado) && !string.IsNullOrEmpty(recibido) &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recibido), Encoding.UTF8.GetBytes(esperado));
}
