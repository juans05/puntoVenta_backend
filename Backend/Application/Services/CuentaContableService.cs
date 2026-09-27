using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class CuentaContableService
{
    private readonly ICuentaContableRepository _repo;

    public CuentaContableService(ICuentaContableRepository repo)
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

    public async Task<MessageResult<object>> Crear(CrearCuentaContablePayload p) => Resolver(await _repo.Crear(p));
    public async Task<MessageResult<object>> Actualizar(int id, ActualizarCuentaContablePayload p) => Resolver(await _repo.Actualizar(id, p));
    public async Task<MessageResult<object>> Listar(bool incluirInactivas) => Resolver(await _repo.Listar(incluirInactivas));
    public async Task<MessageResult<object>> Obtener(int id) => Resolver(await _repo.Obtener(id));
}
