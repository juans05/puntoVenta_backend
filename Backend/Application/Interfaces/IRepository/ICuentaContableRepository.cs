using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface ICuentaContableRepository
{
    Task<(ServiceStatus, CuentaContableDto?, string)> Crear(CrearCuentaContablePayload payload);
    Task<(ServiceStatus, CuentaContableDto?, string)> Actualizar(int id, ActualizarCuentaContablePayload payload);
    Task<(ServiceStatus, List<CuentaContableDto>?, string)> Listar(bool incluirInactivas = false);
    Task<(ServiceStatus, CuentaContableDto?, string)> Obtener(int id);
    Task<(ServiceStatus, object?, string)> Importar(List<ImportarCuentaContablePayload> filas);

    // Usado por AsientoContableRepository para resolver cuentas por codigo al generar asientos.
    Task<CuentaContableDto?> ObtenerPorCodigo(string codigo);

    Task<(ServiceStatus, object?, string)> ListarCodigosEeffNiif();

    Task<(ServiceStatus, object?, string)> ListarPorDefecto();
    Task<(ServiceStatus, object?, string)> GuardarPorDefecto(string codigo, int? cuentaContableId);
}
