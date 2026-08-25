using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class GastoPublicidadService : IGastoPublicidadService
{
    private readonly IGastoPublicidadRepository _repository;

    public GastoPublicidadService(IGastoPublicidadRepository repository)
    {
        _repository = repository;
    }

    public async Task<MessageResult<object>> Importar(ImportarGastoPublicidadPayload payload)
    {
        var (estado, result, message) = await _repository.Importar(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }
}
