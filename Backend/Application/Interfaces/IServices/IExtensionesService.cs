using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IExtensionesService
    {

        Task<MessageResult<object>> ListarTipoDocumento();
        Task<MessageResult<object>> ListarTipoDocumentoVenta();
        Task<MessageResult<object>> ListarMetodoPago();
        Task<MessageResult<object>> ListarMetodoPagoAdmin();
        Task<MessageResult<object>> CrearMetodoPago(CreateMetodoPagoPayload payload);
        Task<MessageResult<bool>> CambiarEstadoMetodoPago(int id, bool estado);
        Task<MessageResult<object>> ListarNacionalidad();
        Task<MessageResult<object>> ListarRubros();
        Task<MessageResult<object>> ListarSucursales();
        Task<MessageResult<object>> ListarUbigeos();
        Task<MessageResult<object>> CrearSucursal(CreateSucursalPayload payload);
        Task<MessageResult<bool>> ReasignarTenantSucursal(int sucursalId, string tenantKey);
    }
}