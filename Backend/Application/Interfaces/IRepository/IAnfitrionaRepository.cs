using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository
{
    public interface IAnfitrionaRepository
    {
        Task<(ServiceStatus, object?, string)> ListarAnfitrionas(int page, int amount);

        Task<(ServiceStatus, object?, string)> CrearAnfitriona(CreateAnfitrionaPayload payload);

        Task<(ServiceStatus, object?, string)> ActualizarAnfitriona(UpdateAnfitrionaPayload payload);

        Task<(ServiceStatus, object?, string)> EliminarAnfitriona(int idAnfitriona);
    }
}