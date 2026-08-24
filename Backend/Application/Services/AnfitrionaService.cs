using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class AnfitrionaService : IAnfitrionaService
{
    private readonly IAnfitrionaRepository _anfitrionaRepository;

    public AnfitrionaService(IAnfitrionaRepository anfitrionaRepository)
    {
        _anfitrionaRepository = anfitrionaRepository;
    }

    public async Task<MessageResult<object>> ListarAnfitrionas(int page, int amount)
    {
        var (estado, result, message) = await _anfitrionaRepository.ListarAnfitrionas(page, amount);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al listar anfitrionas", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> CrearAnfitriona(CreateAnfitrionaPayload payload)
    {
        var (estado, result, message) = await _anfitrionaRepository.CrearAnfitriona(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al crear anfitriona", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ActualizarAnfitriona(UpdateAnfitrionaPayload payload)
    {
        var (estado, result, message) = await _anfitrionaRepository.ActualizarAnfitriona(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al actualizar anfitriona", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> EliminarAnfitriona(int idAnfitriona)
    {
        var (estado, result, message) = await _anfitrionaRepository.EliminarAnfitriona(idAnfitriona);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al eliminar anfitriona", message);

        return MessageResult<object>.Of(message, result);
    }

    private static HttpStatusCode ErrorStatus(ServiceStatus estado) => estado switch
    {
        ServiceStatus.NotFound => HttpStatusCode.NotFound,
        ServiceStatus.FailedValidation => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError,
    };
}