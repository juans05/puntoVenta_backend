using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces;

public interface IUsersRepository
{
    Task<(ServiceStatus, string, int)> CreateUser(CreateUserPayload payload);
    Task<(ServiceStatus, AuthenticatedUsuarioDto?, string)> GetAllUserAccess(string username);
    Task<(ServiceStatus, object?, string)> GetAllUsers();
    Task<(ServiceStatus, object?, string)> ListarUsuarios();
}