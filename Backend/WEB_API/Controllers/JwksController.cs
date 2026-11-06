using Infrastructure.Identidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

// Clave publica para que las otras apps de la suite (ARM) validen los tokens del staff.
// Solo los campos estandar de una clave RSA publica (RFC 7517); nunca parametros privados.
[ApiController]
[AllowAnonymous]
public class JwksController : ControllerBase
{
    [HttpGet("/.well-known/jwks.json")]
    public IActionResult Jwks([FromServices] FirmadorToken firmador)
        => Ok(new { keys = firmador.Jwks().Keys.Select(k => new { kty = k.Kty, use = k.Use, alg = k.Alg, kid = k.Kid, n = k.N, e = k.E }) });
}
