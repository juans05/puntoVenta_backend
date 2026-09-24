using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

// Cuentas por cobrar (cobrar = true, clientes) y por pagar (cobrar = false, proveedores).
public interface ICuentasRepository
{
    Task<(ServiceStatus, List<SocioSaldoDto>?, string)> Resumen(bool cobrar);
    Task<(ServiceStatus, List<DocumentoSaldoDto>?, string)> Documentos(bool cobrar, int socioId);
    Task<(ServiceStatus, List<AntiguedadDto>?, string)> Antiguedad(bool cobrar);
    Task<(ServiceStatus, PagoCuentaDto?, string)> RegistrarPago(bool cobrar, RegistrarPagoCuentaPayload payload);
    Task<(ServiceStatus, DataCollection<PagoCuentaDto>?, string)> ListarPagos(bool cobrar, PagosCuentaQueryParams payload);
    Task<(ServiceStatus, PagoCuentaDto?, string)> AnularPago(bool cobrar, int id);
}
