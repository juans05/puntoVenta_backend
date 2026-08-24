using AutoMapper;
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository tenantRepository;
        private readonly IMapper mapper;

        public TenantService(ITenantRepository tenantRepository, IMapper mapper)
        {
            this.tenantRepository = tenantRepository;
            this.mapper = mapper;
        }

        public async Task<MessageResult<int>> CreateTenant(string nombre, int Rubro, ConfiguracionRentaPayload? configuracion)
        {

            var (estado, identificador, message) = await tenantRepository.CreateTenant(nombre, Rubro, configuracion);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear tenant", message);

            return MessageResult<int>.Of(message, identificador);

        }

        public async Task<MessageResult<object>> GetConfiguracionRubro(int rubroId)
        {

            var (estado, result, message) = await tenantRepository.GetConfiguracionRubro(rubroId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al obtener configuración del rubro", message);

            return MessageResult<object>.Of(message, result);

        }

        public async Task<MessageResult<object>> SaveConfiguracionRubro(int rubroId, ConfiguracionRentaPayload payload)
        {

            var (estado, result, message) = await tenantRepository.SaveConfiguracionRubro(rubroId, payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al guardar configuración del rubro", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> CreateEmpresa(CreateEmpresaPayload payload)
        {

            var (estado, result, message) = await tenantRepository.CreateEmpresa(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear empresa", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<bool>> AddEmpresaTenant(AddEmpresaPayload payload)
        {

            var (estado, message) = await tenantRepository.AddEmpresaTenant(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al AddEmpresaTenant", message);

            return MessageResult<bool>.Of(message, true);

        }
        public async Task<MessageResult<object>> GetEmpresa(string tenantNombre)
        {

            var (estado, result, message) = await tenantRepository.GetEmpresa(tenantNombre);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error GetEmpresa", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object?>> GetRecursos(string tenant)
        {

            var (estado, result, message) = await tenantRepository.GetRecursos(tenant);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , "Error al obtener recursos", message);

            return MessageResult<object?>.Of(message, result);

        }
        public async Task<MessageResult<object>> ModificarTenant(UpdateTenantPayload payload)
        {

            var (estado, result, message) = await tenantRepository.UpdateTenant(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al Actualizar", message);

            var mapeoProducto = mapper.Map<EmpresaDto>(result);

            return MessageResult<object>.Of(message, mapeoProducto);

        }
        public async Task<MessageResult<object>> ListarTenants()
        {

            var (estado, result, message) = await tenantRepository.GetTenants();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , message, result);

            var mapeo = mapper.Map<List<EmpresaDto>>(result);

            return MessageResult<object>.Of(message, mapeo);

        }
        public async Task<MessageResult<object>> GetAllTenants()
        {

            var (estado, result, message) = await tenantRepository.GetAllTenants();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, result);

            return MessageResult<object>.Of(message, result);

        }

        public async Task<MessageResult<object>> GetTenantsResumen()
        {

            var (estado, result, message) = await tenantRepository.GetTenantsResumen();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

            return MessageResult<object>.Of(message, result);

        }

        public async Task<MessageResult<bool>> SetTenantActivo(int identificador, bool activo)
        {

            var (estado, message) = await tenantRepository.SetTenantActivo(identificador, activo);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , "Error al actualizar el estado del tenant", message);

            return MessageResult<bool>.Of(message, true);

        }

        public async Task<MessageResult<bool>> ReasignarModulos(int identificador)
        {

            var (estado, message) = await tenantRepository.ReasignarModulos(identificador);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , "Error al reasignar módulos", message);

            return MessageResult<bool>.Of(message, true);

        }

    }
}