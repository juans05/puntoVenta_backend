using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IClienteService
    {
        Task<MessageResult<object>> CreateCliente(CreateClientePayload payload);
        Task<MessageResult<object>> EliminarCliente(int clienteId);
        Task<MessageResult<object>> GetClientes(ClientePayload payload);
        Task<MessageResult<object>> UpdateCliente(UpdateClientePayload payload);
    }
}