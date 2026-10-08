using Application.Interfaces.IRepository;
using Domain.Models;
using System.Net;

namespace Application.Services;

public class AsientoContableService
{
    private readonly IAsientoContableRepository _repo;

    public AsientoContableService(IAsientoContableRepository repo)
    {
        _repo = repo;
    }

    private static MessageResult<object> Resolver<T>((ServiceStatus estado, T? result, string message) r)
    {
        if (r.estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                r.estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest
                : r.estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError,
                r.message, r.result);

        return MessageResult<object>.Of(r.message, r.result);
    }

    public async Task<MessageResult<object>> Listar(DateTime? desde, DateTime? hasta, string? origenTipo) => Resolver(await _repo.Listar(desde, hasta, origenTipo));
    public async Task<MessageResult<object>> ObtenerEstadoResultados(DateTime? desde, DateTime? hasta) => Resolver(await _repo.ObtenerEstadoResultados(desde, hasta));
    public async Task<MessageResult<object>> ObtenerBalanceGeneral(DateTime hasta) => Resolver(await _repo.ObtenerBalanceGeneral(hasta));
    public async Task<MessageResult<object>> CrearManual(Domain.Payloads.CrearAsientoManualPayload payload) => Resolver(await _repo.CrearManual(payload));
    public async Task<MessageResult<object>> AnularManual(int id) => Resolver(await _repo.AnularManual(id));
}
