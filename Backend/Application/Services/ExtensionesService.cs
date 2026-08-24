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
