using Application.Abstractions;
using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IPedidoService
{
    Task<MessageResult<PedidoDTO>> CrearPedido(CreatePedidoPayload payload);
    Task<MessageResult<PedidoDTO>> ObtenerPedidoPorId(int id);
    Task<MessageResult<PedidoPublicoDTO>> ObtenerPedidoPublico(string token);
    Task<MessageResult<DataCollection<PedidoDTO>>> ListarPedidos(PedidoQueryParams payload);
    Task<MessageResult<PedidoDTO>> ActualizarEstadoPedido(UpdatePedidoEstadoPayload payload);
    Task<MessageResult<bool>> ReenviarPassword(int pedidoId);
    Task<MessageResult<EtiquetaEnvioDTO>> ObtenerEtiquetaEnvio(int pedidoId);
    Task<MessageResult<bool>> SubmitPedidoPublico(string token, PedidoPublicoSubmitPayload payload);
    Task<MessageResult<DataCollection<PedidoDTO>>> ListarPedidosCliente(int clienteId, PaginationPayload payload);
    Task<MessageResult<PedidoDTO>> ObtenerPedidoCliente(int clienteId, int pedidoId);
}