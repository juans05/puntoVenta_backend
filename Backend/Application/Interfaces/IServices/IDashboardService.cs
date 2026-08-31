using Domain.Models;

namespace Application.Interfaces.IServices;

public interface IDashboardService
{
    Task<MessageResult<object>> Resumen(int dias = 7);

    Task<MessageResult<object>> ReporteMargen(string? startDate, string? endDate);
}