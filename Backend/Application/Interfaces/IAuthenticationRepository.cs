using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces
{
    public interface IAuthenticationRepository
    {
        Task<(ServiceStatus, int, AuthenticationModel)> Token(LoginPayload model);
        Task<(ServiceStatus, int, AuthenticationModel)> CambiarEmpresa(string userId, int tenantId);
        Task<(ServiceStatus, List<Domain.DTO.EmpresaAccesoDto>)> ListarEmpresas(string userId);
    }
}