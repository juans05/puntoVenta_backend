using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class OrdenCompraService
{
    private readonly IOrdenCompraRepository _repo;

    public OrdenCompraService(IOrdenCompraRepository repo)
    {
        _repo = repo;
    }

    // Traduce (estado, resultado, mensaje) del repositorio a MessageResult / ErrorHandler,
    // igual que CompraService.
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

    public async Task<MessageResult<object>> ObtenerConfiguracion() => Resolver(await _repo.ObtenerConfiguracion());
    public async Task<MessageResult<object>> GuardarConfiguracion(ConfiguracionFlujoPayload p) => Resolver(await _repo.GuardarConfiguracion(p));
    public async Task<MessageResult<object>> Crear(CreateOrdenCompraPayload p) => Resolver(await _repo.CrearOrden(p));
    public async Task<MessageResult<object>> Actualizar(int id, CreateOrdenCompraPayload p) => Resolver(await _repo.ActualizarOrden(id, p));
    public async Task<MessageResult<object>> Listar(OrdenCompraQueryParams p) => Resolver(await _repo.ListarOrdenes(p));
    public async Task<MessageResult<object>> Obtener(int id) => Resolver(await _repo.ObtenerOrden(id));
    public async Task<MessageResult<object>> Emitir(int id) => Resolver(await _repo.EmitirOrden(id));
    public async Task<MessageResult<object>> Aprobar(int id, string? usuario) => Resolver(await _repo.AprobarOrden(id, usuario));
    public async Task<MessageResult<object>> Anular(int id) => Resolver(await _repo.AnularOrden(id));
    public async Task<MessageResult<object>> Cerrar(int id, string? motivo) => Resolver(await _repo.CerrarOrden(id, motivo));
    public async Task<MessageResult<object>> Recibir(int id, CreateRecepcionPayload p) => Resolver(await _repo.RegistrarRecepcion(id, p));
    public async Task<MessageResult<object>> AnularRecepcion(int recepcionId) => Resolver(await _repo.AnularRecepcion(recepcionId));
    public async Task<MessageResult<object>> Facturar(int id, FacturarOrdenCompraPayload p) => Resolver(await _repo.FacturarOrden(id, p));
}
