using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class CierreAnualService
{
    private readonly ICierreAnualRepository _repo;

    public CierreAnualService(ICierreAnualRepository repo)
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

    public async Task<MessageResult<object>> Validar(CierreAnualPayload payload) => Resolver(await _repo.Validar(payload));
    public async Task<MessageResult<object>> Ejecutar(CierreAnualPayload payload) => Resolver(await _repo.Ejecutar(payload));
}
