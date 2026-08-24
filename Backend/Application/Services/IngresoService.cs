using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class IngresoService : IIngresoService
{
    private readonly IIngresoRepository _ingresoRepository;

    public IngresoService(IIngresoRepository ingresoRepository)
    {
        _ingresoRepository = ingresoRepository;
    }

    public async Task<MessageResult<object>> CrearIngreso(CreateIngresoPayload payload)
    {
        var (estado, result, message) = await _ingresoRepository.CrearIngreso(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> AnularIngreso(int id)
    {
        var (estado, result, message) = await _ingresoRepository.AnularIngreso(id);

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

    public async Task<MessageResult<object>> ListarIngresos(IngresoQueryParams payload)
    {
        var (estado, result, message) = await _ingresoRepository.ListarIngresos(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }
}