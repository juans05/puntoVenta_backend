using Domain.Models;

namespace Application.Interfaces.IServices;

public interface IDashboardService
{
    Task<MessageResult<object>> Resumen();

    Task<MessageResult<object>> ReporteMargen(string? startDate, string? endDate);
}