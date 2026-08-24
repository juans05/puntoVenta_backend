using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IInventoryService
{
    Task<MessageResult<object>> ListarMovimientos(InventoryMovementQuery payload);

    Task<MessageResult<object>> AjustarStock(CreateAjusteInventarioPayload payload);
}