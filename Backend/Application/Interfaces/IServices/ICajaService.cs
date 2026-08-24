using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface ICajaService
{
    Task<MessageResult<object>> MontoActual(string usuario, int? sucursalId = null);

    Task<MessageResult<object>> AbrirCaja(string monto, int? sucursalId = null);

    Task<MessageResult<object>> CerrarCaja(int? sucursalId = null);

    Task<MessageResult<object>> ReporteCaja(string usuario, string fecha, int? sucursalId = null);

    Task<MessageResult<object>> Retiro(CreateRetiroPayload payload);

    Task<MessageResult<object>> ReporteCajaResumido(string usuario, string fecha, int? sucursalId = null);

    Task<MessageResult<object>> HistoricoCierreCajaUsuario(string fecha, int? sucursalId = null);

    Task<MessageResult<object>> ListarCajas();

    Task<MessageResult<object>> CrearCaja(CreateCajaPayload payload);
}