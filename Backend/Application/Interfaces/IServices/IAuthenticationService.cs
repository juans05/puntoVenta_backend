using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IAuthenticationService
    {
        Task<MessageResult<AuthenticationModel>> GetTokenAsync(LoginPayload request);

    }
}