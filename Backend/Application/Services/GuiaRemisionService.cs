using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class GuiaRemisionService
{
    private readonly IGuiaRemisionRepository _repo;

    public GuiaRemisionService(IGuiaRemisionRepository repo)
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

    public async Task<MessageResult<object>> GenerarDesdeEntrega(int entregaId) => Resolver(await _repo.GenerarDesdeEntrega(entregaId));
    public async Task<MessageResult<object>> Actualizar(int id, ActualizarGuiaRemisionPayload p) => Resolver(await _repo.Actualizar(id, p));
    public async Task<MessageResult<object>> Listar(GuiaRemisionQueryParams p) => Resolver(await _repo.Listar(p));
    public async Task<MessageResult<object>> Obtener(int id) => Resolver(await _repo.Obtener(id));
    public async Task<MessageResult<object>> Anular(int id) => Resolver(await _repo.Anular(id));
}
