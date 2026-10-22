using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IAuthenticationService
    {
        Task<MessageResult<AuthenticationModel>> GetTokenAsync(LoginPayload request);
        Task<MessageResult<AuthenticationModel>> CambiarEmpresaAsync(string userId, int tenantId);
        Task<MessageResult<List<Domain.DTO.EmpresaAccesoDto>>> ListarEmpresasAsync(string userId);

    }
}