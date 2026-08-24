using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using System.Net;

namespace Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<MessageResult<object>> Resumen()
    {
        var (estado, result, message) = await _dashboardRepository.Resumen();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }
}