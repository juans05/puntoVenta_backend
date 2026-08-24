using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IIngresoRepository
{
    Task<(ServiceStatus, IngresoDto?, string)> CrearIngreso(CreateIngresoPayload payload);

    Task<(ServiceStatus, IngresoDto?, string)> AnularIngreso(int id);

    Task<(ServiceStatus, DataCollection<IngresoDto>?, string)> ListarIngresos(IngresoQueryParams payload);
}