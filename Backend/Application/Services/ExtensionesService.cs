using Application.Interfaces;
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services

{
    public class ExtensionesService : IExtensionesService
    {
        private readonly IExtensionesRepository _extensionesRepository;

        public ExtensionesService(IExtensionesRepository extensionesRepository)
        {
            _extensionesRepository = extensionesRepository;
        }

        public async Task<MessageResult<object>> ListarTipoDocumento()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarTipoDocumento();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarTipoDocumentoVenta()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarTipoDocumentoVenta();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }
   
        public async Task<MessageResult<object>> ListarMotivosNota(int tipoDocumentoVentaId)
        {

            var (estado, resp, message) = await _extensionesRepository.ListarMotivosNota(tipoDocumentoVentaId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarTiposIgv()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarTiposIgv();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarUnidadesMedida()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarUnidadesMedida();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarTiposOperacion()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarTiposOperacion();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarMonedas()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarMonedas();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarTiposIgvAdmin()
        {
            var (estado, resp, message) = await _extensionesRepository.ListarTiposIgvAdmin();
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> CrearTipoIgv(CreateTipoIgvPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.CrearTipoIgv(payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ActualizarTipoIgv(int id, UpdateTipoIgvPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.ActualizarTipoIgv(id, payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> CambiarEstadoTipoIgv(int id, bool estado)
        {
            var (status, message) = await _extensionesRepository.CambiarEstadoTipoIgv(id, estado);
            if (status != ServiceStatus.Ok)
                throw new ErrorHandler(status == ServiceStatus.NotFound ? HttpStatusCode.NotFound : status == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, null);
            return MessageResult<bool>.Of(message, true);
        }

        public async Task<MessageResult<object>> ListarUnidadesMedidaAdmin()
        {
            var (estado, resp, message) = await _extensionesRepository.ListarUnidadesMedidaAdmin();
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> CrearUnidadMedida(CreateUnidadMedidaPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.CrearUnidadMedida(payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ActualizarUnidadMedida(int id, UpdateUnidadMedidaPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.ActualizarUnidadMedida(id, payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> CambiarEstadoUnidadMedida(int id, bool estado)
        {
            var (status, message) = await _extensionesRepository.CambiarEstadoUnidadMedida(id, estado);
            if (status != ServiceStatus.Ok)
                throw new ErrorHandler(status == ServiceStatus.NotFound ? HttpStatusCode.NotFound : status == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, null);
            return MessageResult<bool>.Of(message, true);
        }

        public async Task<MessageResult<object>> ListarTiposOperacionAdmin()
        {
            var (estado, resp, message) = await _extensionesRepository.ListarTiposOperacionAdmin();
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> CrearTipoOperacion(CreateTipoOperacionPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.CrearTipoOperacion(payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ActualizarTipoOperacion(int id, UpdateTipoOperacionPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.ActualizarTipoOperacion(id, payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> CambiarEstadoTipoOperacion(int id, bool estado)
        {
            var (status, message) = await _extensionesRepository.CambiarEstadoTipoOperacion(id, estado);
            if (status != ServiceStatus.Ok)
                throw new ErrorHandler(status == ServiceStatus.NotFound ? HttpStatusCode.NotFound : status == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, null);
            return MessageResult<bool>.Of(message, true);
        }

        public async Task<MessageResult<object>> ListarMotivosNotaAdmin()
        {
            var (estado, resp, message) = await _extensionesRepository.ListarMotivosNotaAdmin();
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> CrearMotivoNota(CreateMotivoNotaPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.CrearMotivoNota(payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ActualizarMotivoNota(int id, UpdateMotivoNotaPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.ActualizarMotivoNota(id, payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> CambiarEstadoMotivoNota(int id, bool estado)
        {
            var (status, message) = await _extensionesRepository.CambiarEstadoMotivoNota(id, estado);
            if (status != ServiceStatus.Ok)
                throw new ErrorHandler(status == ServiceStatus.NotFound ? HttpStatusCode.NotFound : status == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, null);
            return MessageResult<bool>.Of(message, true);
        }

        public async Task<MessageResult<object>> ListarMonedasAdmin()
        {
            var (estado, resp, message) = await _extensionesRepository.ListarMonedasAdmin();
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> CrearMoneda(CreateMonedaPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.CrearMoneda(payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ActualizarMoneda(int id, UpdateMonedaPayload payload)
        {
            var (estado, resp, message) = await _extensionesRepository.ActualizarMoneda(id, payload);
            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, resp);
            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> CambiarEstadoMoneda(int id, bool estado)
        {
            var (status, message) = await _extensionesRepository.CambiarEstadoMoneda(id, estado);
            if (status != ServiceStatus.Ok)
                throw new ErrorHandler(status == ServiceStatus.NotFound ? HttpStatusCode.NotFound : status == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError, message, null);
            return MessageResult<bool>.Of(message, true);
        }

        public async Task<MessageResult<object>> ConsultarRuc(string ruc)
        {

            var (estado, resp, message) = await _extensionesRepository.ConsultarRuc(ruc);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ConsultarDni(string dni)
        {

            var (estado, resp, message) = await _extensionesRepository.ConsultarDni(dni);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarMetodoPago()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarMetodoPago();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarMetodoPagoAdmin()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarMetodoPagoAdmin();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(HttpStatusCode.InternalServerError, message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> CrearMetodoPago(CreateMetodoPagoPayload payload)
        {

            var (estado, resp, message) = await _extensionesRepository.CrearMetodoPago(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> CambiarEstadoMetodoPago(int id, bool estado)
        {

            var (status, message) = await _extensionesRepository.CambiarEstadoMetodoPago(id, estado);

            if (status != ServiceStatus.Ok)
                throw new ErrorHandler(
                        status == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , message, null);

            return MessageResult<bool>.Of(message, true);
        }

        public async Task<MessageResult<object>> ListarNacionalidad()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarNacionalidad();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarRubros()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarRubros();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarPaises()
        {
            var (estado, resp, message) = await _extensionesRepository.ListarPaises();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(HttpStatusCode.InternalServerError, message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarSucursales()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarSucursales();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarUbigeos()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarUbigeos();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarSalones(string ubigeoId)
        {

            var (estado, resp, message) = await _extensionesRepository.ListarSalones(ubigeoId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> ListarSalonesAdmin()
        {

            var (estado, resp, message) = await _extensionesRepository.ListarSalonesAdmin();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(HttpStatusCode.InternalServerError, message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<object>> CrearSalon(CreateSalonPayload payload)
        {

            var (estado, resp, message) = await _extensionesRepository.CrearSalon(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> CambiarEstadoSalon(int id, bool estado)
        {

            var (status, message) = await _extensionesRepository.CambiarEstadoSalon(id, estado);

            if (status != ServiceStatus.Ok)
                throw new ErrorHandler(
                        status == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , message, null);

            return MessageResult<bool>.Of(message, true);
        }

        public async Task<MessageResult<object>> CrearSucursal(CreateSucursalPayload payload)
        {

            var (estado, resp, message) = await _extensionesRepository.CrearSucursal(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, resp);

            return MessageResult<object>.Of(message, resp);
        }

        public async Task<MessageResult<bool>> ReasignarTenantSucursal(int sucursalId, string tenantKey)
        {

            var (estado, message) = await _extensionesRepository.ReasignarTenantSucursal(sucursalId, tenantKey);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , "Error al reasignar la sucursal", message);

            return MessageResult<bool>.Of(message, true);
        }

    }
}
