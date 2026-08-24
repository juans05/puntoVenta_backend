using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IAnfitrionaService
    {
        Task<MessageResult<object>> ListarAnfitrionas(int page, int amount);

        Task<MessageResult<object>> CrearAnfitriona(CreateAnfitrionaPayload payload);

        Task<MessageResult<object>> ActualizarAnfitriona(UpdateAnfitrionaPayload payload);

        Task<MessageResult<object>> EliminarAnfitriona(int idAnfitriona);
    }
}