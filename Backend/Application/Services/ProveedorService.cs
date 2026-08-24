using AutoMapper;
using AutoMapper;
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly IProveedorRepository  proveedorRepository;
        private readonly IMapper mapper;

        public ProveedorService(IProveedorRepository proveedorRepository, IMapper mapper)
        {
            this.proveedorRepository = proveedorRepository;
            this.mapper = mapper; 
        }

        public async Task<MessageResult<object>> CrearProveedor(CreateProveedorPayload payload)
        {

            var (estado, result, message) = await proveedorRepository.CrearProveedor(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> ModificarProveedor(UpdateProveedorPayload payload)
        {

            var (estado, result, message) = await proveedorRepository.UpdateProveedor(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> EliminarProveedor(int proveedorId)
        {

            var (estado, result, message) = await proveedorRepository.DeleteProveedor(proveedorId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, null);

        }
        public async Task<MessageResult<object>> ListarProveedor(ProveedorPayload payload)
        {

            var (estado, result, message) = await proveedorRepository.GetProveedor(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    ,  message, result);

            return MessageResult<object>.Of(message, result);

        }


    }
}