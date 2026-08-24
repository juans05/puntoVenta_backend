using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class CierreDiarioService : ICierreDiarioService
{
    private readonly ICierreDiarioRepository _cierreDiarioRepository;

    public CierreDiarioService(ICierreDiarioRepository cierreDiarioRepository)
    {
        _cierreDiarioRepository = cierreDiarioRepository;
    }

    public async Task<MessageResult<object>> ResumenDia()
    {
        var (estado, result, message) = await _cierreDiarioRepository.ResumenDia();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> CerrarDia(CierreDiarioPayload payload)
    {
        var (estado, result, message) = await _cierreDiarioRepository.CerrarDia(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ListarCierres(CierreDiarioQueryParams payload)
    {
        var (estado, result, message) = await _cierreDiarioRepository.ListarCierres(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }
}