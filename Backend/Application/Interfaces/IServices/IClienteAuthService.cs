using Application.Abstractions;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IClienteAuthService
{
    Task<MessageResult<ClienteLoginResult>> Login(ClienteLoginPayload payload);
    Task<MessageResult<bool>> ValidarToken(string token);
}

public class ClienteLoginResult
{
    public string Token { get; set; } = null!;
    public int ClienteId { get; set; }
    public string Email { get; set; } = null!;
    public DateTime Expiracion { get; set; }
}