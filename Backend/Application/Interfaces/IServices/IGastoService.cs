using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IGastoService
{
    Task<MessageResult<object>> CrearGasto(CreateGastoPayload payload);

    Task<MessageResult<object>> AnularGasto(int id);

    Task<MessageResult<object>> ListarGastos(GastoQueryParams payload);

    Task<MessageResult<object>> ListarCategorias();

    Task<MessageResult<object>> CrearCategoria(CreateCategoriaGastoPayload payload);

    Task<MessageResult<bool>> CambiarEstadoCategoria(int id, bool estado);
}
