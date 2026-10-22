using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;
using WEB_API.Authentication;
using System.Security.Claims;

namespace WEB_API.Controllers;

[Route("api/autenticacion")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;


    public AuthController(IAuthenticationService authenticationService)
    {
        _authService = authenticationService;
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<IActionResult> GetTokenAsync([FromBody] LoginPayload model) => Ok(await _authService.GetTokenAsync(model));

    [Authorize(AuthenticationSchemes = "Staff")]
    [HttpGet("@me")]
    public IActionResult MeData() => Ok(User.GetUser());

    // nameid del JWT (JwtRegisteredClaimNames.NameId) llega mapeado a NameIdentifier.
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? "";

    [Authorize(AuthenticationSchemes = "Staff")]
    [HttpGet("empresas")]
    public async Task<IActionResult> ListarEmpresas() => Ok(await _authService.ListarEmpresasAsync(UserId));

    [Authorize(AuthenticationSchemes = "Staff")]
    [HttpPost("cambiar-empresa/{tenantId:int}")]
    public async Task<IActionResult> CambiarEmpresa(int tenantId) => Ok(await _authService.CambiarEmpresaAsync(UserId, tenantId));


}