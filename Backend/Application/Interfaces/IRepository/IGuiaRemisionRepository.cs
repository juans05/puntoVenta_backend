using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IGuiaRemisionRepository
{
    Task<(ServiceStatus, GuiaRemisionDto?, string)> GenerarDesdeEntrega(int entregaId);
    Task<(ServiceStatus, GuiaRemisionDto?, string)> Actualizar(int id, ActualizarGuiaRemisionPayload payload);
    Task<(ServiceStatus, DataCollection<GuiaRemisionDto>?, string)> Listar(GuiaRemisionQueryParams payload);
    Task<(ServiceStatus, GuiaRemisionDto?, string)> Obtener(int id);
    Task<(ServiceStatus, GuiaRemisionDto?, string)> Anular(int id);
    // Usado por PedidoVentaRepository antes de anular una entrega: bloquea si tiene guia activa.
    Task<bool> TieneGuiaActiva(int entregaId);
}
