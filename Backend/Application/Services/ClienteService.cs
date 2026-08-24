using AutoMapper;
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository clienteRepository;
        private readonly IMapper mapper;

        public ClienteService(IClienteRepository clienteRepository, IMapper mapper)
        {
            this.clienteRepository = clienteRepository;
            this.mapper = mapper;
        }

        public async Task<MessageResult<object>> CreateCliente(CreateClientePayload payload)
        {

            var (estado, result, message) = await clienteRepository.CreateCliente(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> UpdateCliente(UpdateClientePayload payload)
        {

            var (estado, result, message) = await clienteRepository.UpdateCliente(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al actualizar", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> EliminarCliente(int clienteId)
        {

            var (estado, result, message) = await clienteRepository.EliminarCliente(clienteId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al eliminar", message);

            return MessageResult<object>.Of(message, null);

        }
        public async Task<MessageResult<object>> GetClientes(ClientePayload payload)
        {

            var (estado, result, message) = await clienteRepository.GetClientes(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , message, result);

            return MessageResult<object>.Of(message, result);

        }


    }
}