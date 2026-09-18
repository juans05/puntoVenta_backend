using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository
{
    public interface IExtensionesRepository
    {
        Task<(ServiceStatus, object?, string)> ListarTipoDocumento();

        Task<(ServiceStatus, object?, string)> ListarTipoDocumentoVenta();

        Task<(ServiceStatus, object?, string)> ListarMotivosNota(int tipoDocumentoVentaId);
        Task<(ServiceStatus, object?, string)> ListarTiposIgv();
        Task<(ServiceStatus, object?, string)> ListarUnidadesMedida();
        Task<(ServiceStatus, object?, string)> ListarTiposOperacion();
        Task<(ServiceStatus, object?, string)> ListarMonedas();

        // CRUD de catalogos "base SUNAT + personalizables por tenant": Listar{X}Admin trae
        // tanto las filas base (TenantId null) como las propias del tenant; Crear/Actualizar/
        // CambiarEstado solo pueden tocar las propias (ver guard en la implementacion).
        Task<(ServiceStatus, object?, string)> ListarTiposIgvAdmin();
        Task<(ServiceStatus, object?, string)> CrearTipoIgv(CreateTipoIgvPayload payload);
        Task<(ServiceStatus, object?, string)> ActualizarTipoIgv(int id, UpdateTipoIgvPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoTipoIgv(int id, bool estado);

        Task<(ServiceStatus, object?, string)> ListarUnidadesMedidaAdmin();
        Task<(ServiceStatus, object?, string)> CrearUnidadMedida(CreateUnidadMedidaPayload payload);
        Task<(ServiceStatus, object?, string)> ActualizarUnidadMedida(int id, UpdateUnidadMedidaPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoUnidadMedida(int id, bool estado);

        Task<(ServiceStatus, object?, string)> ListarTiposOperacionAdmin();
        Task<(ServiceStatus, object?, string)> CrearTipoOperacion(CreateTipoOperacionPayload payload);
        Task<(ServiceStatus, object?, string)> ActualizarTipoOperacion(int id, UpdateTipoOperacionPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoTipoOperacion(int id, bool estado);

        Task<(ServiceStatus, object?, string)> ListarMotivosNotaAdmin();
        Task<(ServiceStatus, object?, string)> CrearMotivoNota(CreateMotivoNotaPayload payload);
        Task<(ServiceStatus, object?, string)> ActualizarMotivoNota(int id, UpdateMotivoNotaPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoMotivoNota(int id, bool estado);

        Task<(ServiceStatus, object?, string)> ListarMonedasAdmin();
        Task<(ServiceStatus, object?, string)> CrearMoneda(CreateMonedaPayload payload);
        Task<(ServiceStatus, object?, string)> ActualizarMoneda(int id, UpdateMonedaPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoMoneda(int id, bool estado);

        Task<(ServiceStatus, object?, string)> ConsultarRuc(string ruc);
        Task<(ServiceStatus, object?, string)> ConsultarDni(string dni);

        Task<(ServiceStatus, object?, string)> ListarMetodoPago();
        Task<(ServiceStatus, object?, string)> ListarMetodoPagoAdmin();
        Task<(ServiceStatus, object?, string)> CrearMetodoPago(CreateMetodoPagoPayload payload);
        Task<(ServiceStatus, string)> CambiarEstadoMetodoPago(int id, bool estado);
        Task<(ServiceStatus, object?, string)> ListarNacionalidad();
        Task<(ServiceStatus, object?, string)> ListarRubros();
        Task<(ServiceStatus, object?, string)> ListarPaises();
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