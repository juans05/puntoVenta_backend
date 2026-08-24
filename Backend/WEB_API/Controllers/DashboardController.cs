using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;

namespace WEB_API.Controllers;

[Route("api/dashboard")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Resumen() => Ok(await _dashboardService.Resumen());
}