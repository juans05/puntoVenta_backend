using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IUserService
    {
        Task<MessageResult<string>> CreateUserAsync(CreateUserPayload request);

        Task<MessageResult<object>> GetAllUserAccess(string username);

        Task<MessageResult<object>> GetAllUsers();
    Task<MessageResult<object>> ListarUsuarios();

    }
}