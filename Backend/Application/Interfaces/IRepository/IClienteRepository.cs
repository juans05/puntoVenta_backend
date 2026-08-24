using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository
{
    public interface IClienteRepository
    {
        Task<(ServiceStatus, ClienteDto?, string)> CreateCliente(CreateClientePayload payload);
        Task<(ServiceStatus, bool, string)> EliminarCliente(int ClienteId);
        Task<(ServiceStatus, DataCollection<ClienteDto>?, string)> GetClientes(ClientePayload payload);
        Task<(ServiceStatus, ClienteDto?, string)> UpdateCliente(UpdateClientePayload payload);
    }
}