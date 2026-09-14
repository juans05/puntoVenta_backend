using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IExtensionesService
    {

        Task<MessageResult<object>> ListarTipoDocumento();
        Task<MessageResult<object>> ListarTipoDocumentoVenta();
        Task<MessageResult<object>> ConsultarRuc(string ruc);
        Task<MessageResult<object>> ConsultarDni(string dni);
        Task<MessageResult<object>> ListarMetodoPago();
        Task<MessageResult<object>> ListarMetodoPagoAdmin();
        Task<MessageResult<object>> CrearMetodoPago(CreateMetodoPagoPayload payload);
        Task<MessageResult<bool>> CambiarEstadoMetodoPago(int id, bool estado);
        Task<MessageResult<object>> ListarNacionalidad();
        Task<MessageResult<object>> ListarRubros();
        Task<MessageResult<object>> ListarSucursales();
        Task<MessageResult<object>> ListarUbigeos();
        Task<MessageResult<object>> ListarSalones(string ubigeoId);
        Task<MessageResult<object>> ListarSalonesAdmin();
        Task<MessageResult<object>> CrearSalon(CreateSalonPayload payload);
        Task<MessageResult<bool>> CambiarEstadoSalon(int id, bool estado);
        Task<MessageResult<object>> CrearSucursal(CreateSucursalPayload payload);
        Task<MessageResult<bool>> ReasignarTenantSucursal(int sucursalId, string tenantKey);
    }
}