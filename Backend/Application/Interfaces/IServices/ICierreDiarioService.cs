using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface ICierreDiarioService
{
    Task<MessageResult<object>> ResumenDia();

    Task<MessageResult<object>> CerrarDia(CierreDiarioPayload payload);

    Task<MessageResult<object>> ListarCierres(CierreDiarioQueryParams payload);
}