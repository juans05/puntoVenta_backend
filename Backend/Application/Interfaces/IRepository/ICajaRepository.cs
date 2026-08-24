using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface ICajaRepository
{

    Task<(ServiceStatus, object?, string)> MontoActual(string usuario, int? sucursalId = null);

    Task<(ServiceStatus, object?, string)> AbrirCaja(string monto, int? sucursalId = null);

    Task<(ServiceStatus, object?, string)> CerrarCaja(int? sucursalId = null);

    Task<(ServiceStatus, object, string)> ReporteCaja(string usuario, string fecha, int? sucursalId = null);

    Task<(ServiceStatus, object?, string)> Retiro(CreateRetiroPayload payload);

    Task<(ServiceStatus, object?, string)> HistoricoCierreCajaUsuario(string fecha, int? sucursalId = null);

    Task<(ServiceStatus, object?, string)> ReporteCajaResumido(string usuario, string fecha, int? sucursalId = null);

    Task<(ServiceStatus, object?, string)> ListarCajas();

    Task<(ServiceStatus, object?, string)> CrearCaja(CreateCajaPayload payload);
}