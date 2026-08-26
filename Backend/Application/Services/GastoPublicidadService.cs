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

    public async Task<MessageResult<object>> CalcularRoi(GastoPublicidadRoiQueryParams payload)
    {
        var (estado, result, message) = await _repository.CalcularRoi(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> Listar(GastoPublicidadQueryParams payload)
    {
        var (estado, result, message) = await _repository.Listar(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ObtenerMapeosAnuncios(ObtenerMapeosAnunciosPayload payload)
    {
        var (estado, result, message) = await _repository.ObtenerMapeosAnuncios(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }
}
