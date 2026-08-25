using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IGastoPublicidadService
{
    Task<MessageResult<object>> Importar(ImportarGastoPublicidadPayload payload);
    Task<MessageResult<object>> CalcularRoi(GastoPublicidadRoiQueryParams payload);
    Task<MessageResult<object>> Listar(GastoPublicidadQueryParams payload);
}
