using Domain.Common;
using Domain.DTO;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IInventoryRepository
{
    Task<(ServiceStatus, InventoryMovementDto?, string)> RegistrarMovimiento(int productoId, TipoMovimientoInventario tipo, int cantidad, string? referenciaTipo = null, int? referenciaId = null);

    Task<(ServiceStatus, InventoryMovementDto?, string)> AjustarStock(CreateAjusteInventarioPayload payload);

    Task<(ServiceStatus, DataCollection<InventoryMovementDto>?, string)> ListarMovimientos(InventoryMovementQuery payload);
}