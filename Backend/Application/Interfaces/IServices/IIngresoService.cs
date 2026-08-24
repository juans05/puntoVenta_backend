using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IIngresoService
{
    Task<MessageResult<object>> CrearIngreso(CreateIngresoPayload payload);

    Task<MessageResult<object>> AnularIngreso(int id);

    Task<MessageResult<object>> ListarIngresos(IngresoQueryParams payload);
}