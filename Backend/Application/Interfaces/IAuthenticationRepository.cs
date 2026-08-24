using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces
{
    public interface IAuthenticationRepository
    {
        Task<(ServiceStatus, int, AuthenticationModel)> Token(LoginPayload model);
    }
}