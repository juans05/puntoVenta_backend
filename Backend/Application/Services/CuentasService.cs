using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class CuentasService
{
    private readonly ICuentasRepository _repo;

    public CuentasService(ICuentasRepository repo)
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

    public async Task<MessageResult<object>> Resumen(bool cobrar) => Resolver(await _repo.Resumen(cobrar));
    public async Task<MessageResult<object>> Documentos(bool cobrar, int socioId) => Resolver(await _repo.Documentos(cobrar, socioId));
    public async Task<MessageResult<object>> Antiguedad(bool cobrar) => Resolver(await _repo.Antiguedad(cobrar));
    public async Task<MessageResult<object>> RegistrarPago(bool cobrar, RegistrarPagoCuentaPayload p) => Resolver(await _repo.RegistrarPago(cobrar, p));
    public async Task<MessageResult<object>> ListarPagos(bool cobrar, PagosCuentaQueryParams p) => Resolver(await _repo.ListarPagos(cobrar, p));
    public async Task<MessageResult<object>> AnularPago(bool cobrar, int id) => Resolver(await _repo.AnularPago(cobrar, id));
}
