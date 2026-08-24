using Domain.DTO;

namespace Application.Interfaces.IServices;

public interface IAIService
{
    Task<IntentResult> Procesar(string texto, string? username, int? sucursalId);
}