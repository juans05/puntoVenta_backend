using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface ICierreDiarioRepository
{
    Task<(ServiceStatus, ResumenDia?, string)> ResumenDia(int? sucursalId = null);

    Task<(ServiceStatus, CierreDiarioDto?, string)> CerrarDia(CierreDiarioPayload payload, int? sucursalId = null);

    Task<(ServiceStatus, DataCollection<CierreDiarioDto>?, string)> ListarCierres(CierreDiarioQueryParams payload);
}