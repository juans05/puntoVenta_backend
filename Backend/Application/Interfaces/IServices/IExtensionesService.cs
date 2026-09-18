using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IExtensionesService
    {

        Task<MessageResult<object>> ListarTipoDocumento();
        Task<MessageResult<object>> ListarTipoDocumentoVenta();
        Task<MessageResult<object>> ListarMotivosNota(int tipoDocumentoVentaId);
        Task<MessageResult<object>> ListarTiposIgv();
        Task<MessageResult<object>> ListarUnidadesMedida();
        Task<MessageResult<object>> ListarTiposOperacion();
        Task<MessageResult<object>> ListarMonedas();

        Task<MessageResult<object>> ListarTiposIgvAdmin();
        Task<MessageResult<object>> CrearTipoIgv(CreateTipoIgvPayload payload);
        Task<MessageResult<object>> ActualizarTipoIgv(int id, UpdateTipoIgvPayload payload);
        Task<MessageResult<bool>> CambiarEstadoTipoIgv(int id, bool estado);

        Task<MessageResult<object>> ListarUnidadesMedidaAdmin();
        Task<MessageResult<object>> CrearUnidadMedida(CreateUnidadMedidaPayload payload);
        Task<MessageResult<object>> ActualizarUnidadMedida(int id, UpdateUnidadMedidaPayload payload);
        Task<MessageResult<bool>> CambiarEstadoUnidadMedida(int id, bool estado);

        Task<MessageResult<object>> ListarTiposOperacionAdmin();
        Task<MessageResult<object>> CrearTipoOperacion(CreateTipoOperacionPayload payload);
        Task<MessageResult<object>> ActualizarTipoOperacion(int id, UpdateTipoOperacionPayload payload);
        Task<MessageResult<bool>> CambiarEstadoTipoOperacion(int id, bool estado);

        Task<MessageResult<object>> ListarMotivosNotaAdmin();
        Task<MessageResult<object>> CrearMotivoNota(CreateMotivoNotaPayload payload);
        Task<MessageResult<object>> ActualizarMotivoNota(int id, UpdateMotivoNotaPayload payload);
        Task<MessageResult<bool>> CambiarEstadoMotivoNota(int id, bool estado);

        Task<MessageResult<object>> ListarMonedasAdmin();
        Task<MessageResult<object>> CrearMoneda(CreateMonedaPayload payload);
        Task<MessageResult<object>> ActualizarMoneda(int id, UpdateMonedaPayload payload);
        Task<MessageResult<bool>> CambiarEstadoMoneda(int id, bool estado);

        Task<MessageResult<object>> ConsultarRuc(string ruc);
        Task<MessageResult<object>> ConsultarDni(string dni);
        Task<MessageResult<object>> ListarMetodoPago();
        Task<MessageResult<object>> ListarMetodoPagoAdmin();
        Task<MessageResult<object>> CrearMetodoPago(CreateMetodoPagoPayload payload);
        Task<MessageResult<bool>> CambiarEstadoMetodoPago(int id, bool estado);
        Task<MessageResult<object>> ListarNacionalidad();
        Task<MessageResult<object>> ListarRubros();
        Task<MessageResult<object>> ListarPaises();
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