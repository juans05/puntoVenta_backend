using Domain.Models;

namespace Application.Interfaces.IServices;

public interface IDashboardService
{
    Task<MessageResult<object>> Resumen();
}