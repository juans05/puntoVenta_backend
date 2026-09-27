using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class DepartamentoService
{
    private readonly IDepartamentoRepository _repo;

    public DepartamentoService(IDepartamentoRepository repo)
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

    public async Task<MessageResult<object>> Crear(CrearDepartamentoPayload p) => Resolver(await _repo.Crear(p));
    public async Task<MessageResult<object>> Actualizar(int id, CrearDepartamentoPayload p) => Resolver(await _repo.Actualizar(id, p));
    public async Task<MessageResult<object>> Listar() => Resolver(await _repo.Listar());
    public async Task<MessageResult<object>> Obtener(int id) => Resolver(await _repo.Obtener(id));
    public async Task<MessageResult<object>> AsignarAprobadores(int id, AsignarAprobadoresPayload p) => Resolver(await _repo.AsignarAprobadores(id, p));
}
