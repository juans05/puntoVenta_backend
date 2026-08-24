using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;
using WEB_API.Authentication;

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

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("@me")]
    public IActionResult MeData() => Ok(User.GetUser());


}