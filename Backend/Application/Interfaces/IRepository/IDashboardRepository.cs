using Domain.DTO;
using Domain.Models;

namespace Application.Interfaces.IRepository;

public interface IDashboardRepository
{
    Task<(ServiceStatus, DashboardResumenDto?, string)> Resumen(int dias = 7);

    Task<(ServiceStatus, ReporteMargenDto?, string)> ReporteMargen(string? startDate, string? endDate);
}