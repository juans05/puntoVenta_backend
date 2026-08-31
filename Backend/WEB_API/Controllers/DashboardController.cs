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
    public async Task<IActionResult> Resumen([FromQuery] int dias = 7) => Ok(await _dashboardService.Resumen(dias));

    [HttpGet("reporte-margen")]
    public async Task<IActionResult> ReporteMargen([FromQuery] string? startDate, [FromQuery] string? endDate) => Ok(await _dashboardService.ReporteMargen(startDate, endDate));
}