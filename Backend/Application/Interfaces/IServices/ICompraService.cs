using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface ICompraService
{
    Task<MessageResult<object>> CrearCompra(CreateCompraPayload payload);

    Task<MessageResult<object>> AnularCompra(int id);

    Task<MessageResult<object>> ListarCompras(CompraQueryParams payload);

    Task<MessageResult<object>> ObtenerCompra(int id);

    Task<MessageResult<bool>> ActualizarFechaCompra(int id, DateTime fecha);

    Task<MessageResult<object>> ActualizarCompra(int id, CreateCompraPayload payload);
}