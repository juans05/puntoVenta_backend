using Application.Interfaces.IRepository;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class PedidoVentaService
{
    private readonly IPedidoVentaRepository _repo;

    public PedidoVentaService(IPedidoVentaRepository repo)
    {
        _repo = repo;
    }

    // Igual que OrdenCompraService: traduce (estado, resultado, mensaje) a MessageResult / ErrorHandler.
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

    public async Task<MessageResult<object>> Crear(CreatePedidoVentaPayload p) => Resolver(await _repo.Crear(p));
    public async Task<MessageResult<object>> DesdeCotizacion(int cotizacionId) => Resolver(await _repo.CrearDesdeCotizacion(cotizacionId));
    public async Task<MessageResult<object>> Actualizar(int id, CreatePedidoVentaPayload p) => Resolver(await _repo.Actualizar(id, p));
    public async Task<MessageResult<object>> Listar(PedidoVentaQueryParams p) => Resolver(await _repo.Listar(p));
    public async Task<MessageResult<object>> Obtener(int id) => Resolver(await _repo.Obtener(id));
    public async Task<MessageResult<object>> Confirmar(int id) => Resolver(await _repo.Confirmar(id));
    public async Task<MessageResult<object>> Anular(int id) => Resolver(await _repo.Anular(id));
    public async Task<MessageResult<object>> Cerrar(int id, string? motivo) => Resolver(await _repo.Cerrar(id, motivo));
    public async Task<MessageResult<object>> Entregar(int id, CreateEntregaPayload p) => Resolver(await _repo.RegistrarEntrega(id, p));
    public async Task<MessageResult<object>> AnularEntrega(int entregaId) => Resolver(await _repo.AnularEntrega(entregaId));
}
