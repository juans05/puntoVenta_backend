using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IPedidoVentaRepository
{
    Task<(ServiceStatus, PedidoVentaDto?, string)> Crear(CreatePedidoVentaPayload payload);
    Task<(ServiceStatus, PedidoVentaDto?, string)> CrearDesdeCotizacion(int cotizacionId);
    Task<(ServiceStatus, PedidoVentaDto?, string)> Actualizar(int id, CreatePedidoVentaPayload payload);
    Task<(ServiceStatus, DataCollection<PedidoVentaDto>?, string)> Listar(PedidoVentaQueryParams payload);
    Task<(ServiceStatus, PedidoVentaDto?, string)> Obtener(int id);
    Task<(ServiceStatus, PedidoVentaDto?, string)> Confirmar(int id);
    Task<(ServiceStatus, PedidoVentaDto?, string)> Anular(int id);
    Task<(ServiceStatus, PedidoVentaDto?, string)> Cerrar(int id, string? motivo);
    Task<(ServiceStatus, PedidoVentaDto?, string)> RegistrarEntrega(int pedidoId, CreateEntregaPayload payload);
    Task<(ServiceStatus, PedidoVentaDto?, string)> AnularEntrega(int entregaId);
}
