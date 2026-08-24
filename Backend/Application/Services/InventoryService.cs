using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<MessageResult<object>> ListarMovimientos(InventoryMovementQuery payload)
    {
        var (estado, result, message) = await _inventoryRepository.ListarMovimientos(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> AjustarStock(CreateAjusteInventarioPayload payload)
    {
        var (estado, result, message) = await _inventoryRepository.AjustarStock(payload);

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
}