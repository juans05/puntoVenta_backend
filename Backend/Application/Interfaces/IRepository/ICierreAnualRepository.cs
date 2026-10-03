using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

// Asistente de cierre de año fiscal (5 fases, ver Plan de Cuentas -> Cuenta Cierre).
public interface ICierreAnualRepository
{
    Task<(ServiceStatus, CierreAnualValidacionDto?, string)> Validar(CierreAnualPayload payload);
    Task<(ServiceStatus, CierreAnualResultadoDto?, string)> Ejecutar(CierreAnualPayload payload);
}
