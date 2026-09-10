using Application.Abstractions;
using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IPedidoRepository
{
    Task<(ServiceStatus, PedidoDTO?, string)> CrearPedido(CreatePedidoPayload payload);
    Task<(ServiceStatus, PedidoDTO?, string)> ObtenerPedidoPorId(int id);
    Task<(ServiceStatus, PedidoPublicoDTO?, string)> ObtenerPedidoPublico(string token);
    Task<(ServiceStatus, DataCollection<PedidoDTO>?, string)> ListarPedidos(PedidoQueryParams payload);
    Task<(ServiceStatus, PedidoDTO?, string)> ActualizarEstadoPedido(UpdatePedidoEstadoPayload payload);
    Task<(ServiceStatus, bool, string)> ReenviarPassword(int pedidoId);
    Task<(ServiceStatus, EtiquetaEnvioDTO?, string)> ObtenerEtiquetaEnvio(int pedidoId);
    Task<(ServiceStatus, SubmitPedidoResult, string)> SubmitPedidoPublico(string token, PedidoPublicoSubmitPayload payload);
    Task<(ServiceStatus, bool, string)> ActualizarPasswordEnviado(int pedidoId, bool enviado);
    Task<(ServiceStatus, DataCollection<PedidoDTO>?, string)> ListarPedidosCliente(int clienteId, PaginationPayload payload);
    Task<(ServiceStatus, PedidoDTO?, string)> ObtenerPedidoCliente(int clienteId, int pedidoId);
}