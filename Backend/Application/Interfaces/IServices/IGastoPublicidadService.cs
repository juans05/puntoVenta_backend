using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IGastoPublicidadService
{
    Task<MessageResult<object>> Importar(ImportarGastoPublicidadPayload payload);
}
