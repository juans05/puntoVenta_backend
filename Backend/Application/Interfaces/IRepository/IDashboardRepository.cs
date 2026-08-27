using Domain.DTO;
using Domain.Models;

namespace Application.Interfaces.IRepository;

public interface IDashboardRepository
{
    Task<(ServiceStatus, DashboardResumenDto?, string)> Resumen();

    Task<(ServiceStatus, ReporteMargenDto?, string)> ReporteMargen(string? startDate, string? endDate);
}