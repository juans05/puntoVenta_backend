using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository
{
    public interface IExtensionesRepository
    {
        Task<(ServiceStatus, object?, string)> ListarTipoDocumento();

        Task<(ServiceStatus, object?, string)> ListarTipoDocumentoVenta();

        Task<(ServiceStatus, object?, string)> ListarMetodoPago();
        Task<(ServiceStatus, object?, string)> ListarMetodoPagoAdmin();
        Task<(ServiceStatus, object?, string)> CrearMetodoPago(CreateMetodoPagoPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoMetodoPago(int id, bool estado);
        Task<(ServiceStatus, object?, string)> ListarNacionalidad();
        Task<(ServiceStatus, object?, string)> ListarRubros();
        Task<(ServiceStatus, object?, string)> ListarSucursales();
        Task<(ServiceStatus, object?, string)> ListarUbigeos();
        Task<(ServiceStatus, object?, string)> ListarSalones(string ubigeoId);
        Task<(ServiceStatus, object?, string)> ListarSalonesAdmin();
        Task<(ServiceStatus, object?, string)> CrearSalon(CreateSalonPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoSalon(int id, bool estado);
        Task<(ServiceStatus, object?, string)> CrearSucursal(CreateSucursalPayload payload);
        Task<(ServiceStatus, string)> ReasignarTenantSucursal(int sucursalId, string tenantKey);
    }
}