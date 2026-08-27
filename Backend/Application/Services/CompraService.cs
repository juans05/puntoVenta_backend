using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class CompraService : ICompraService
{
    private readonly ICompraRepository _compraRepository;

    public CompraService(ICompraRepository compraRepository)
    {
        _compraRepository = compraRepository;
    }

    public async Task<MessageResult<object>> CrearCompra(CreateCompraPayload payload)
    {
        var (estado, result, message) = await _compraRepository.CrearCompra(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> AnularCompra(int id)
    {
        var (estado, result, message) = await _compraRepository.AnularCompra(id);

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

    public async Task<MessageResult<object>> ListarCompras(CompraQueryParams payload)
    {
        var (estado, result, message) = await _compraRepository.ListarCompras(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ObtenerCompra(int id)
    {
        var (estado, result, message) = await _compraRepository.ObtenerCompra(id);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<bool>> ActualizarFechaCompra(int id, DateTime fecha)
    {
        var (estado, message) = await _compraRepository.ActualizarFechaCompra(id, fecha);

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
}