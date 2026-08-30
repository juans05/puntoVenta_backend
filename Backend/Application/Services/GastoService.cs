using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class GastoService : IGastoService
{
    private readonly IGastoRepository _gastoRepository;

    public GastoService(IGastoRepository gastoRepository)
    {
        _gastoRepository = gastoRepository;
    }

    public async Task<MessageResult<object>> CrearGasto(CreateGastoPayload payload)
    {
        var (estado, result, message) = await _gastoRepository.CrearGasto(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> AnularGasto(int id)
    {
        var (estado, result, message) = await _gastoRepository.AnularGasto(id);

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

    public async Task<MessageResult<bool>> ActualizarFechaGasto(int id, DateTime fecha)
    {
        var (estado, message) = await _gastoRepository.ActualizarFechaGasto(id, fecha);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }

    public async Task<MessageResult<object>> ListarGastos(GastoQueryParams payload)
    {
        var (estado, result, message) = await _gastoRepository.ListarGastos(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ListarCategorias()
    {
        var (estado, result, message) = await _gastoRepository.ListarCategorias();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> CrearCategoria(CreateCategoriaGastoPayload payload)
    {
        var (estado, result, message) = await _gastoRepository.CrearCategoria(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<bool>> CambiarEstadoCategoria(int id, bool estado)
    {
        var (status, message) = await _gastoRepository.CambiarEstadoCategoria(id, estado);

        if (status != ServiceStatus.Ok)
            throw new ErrorHandler(
                    status == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }

    public async Task<MessageResult<object>> Importar(ImportarGastoPayload payload)
    {
        var (estado, result, message) = await _gastoRepository.Importar(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }
}
