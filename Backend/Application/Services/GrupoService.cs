using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
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
    public class GrupoService : IGrupoService
    {
        private readonly IGrupoRepository grupoRepository;

        public GrupoService(IGrupoRepository grupoRepository)
        {
            this.grupoRepository = grupoRepository;
        }

        public async Task<MessageResult<object>> CrearGrupo(CreateGrupoPayload payload)
        {

            var (estado, result, message) = await grupoRepository.CrearGrupo(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> ModificarGrupo(UpdateGrupoPayload payload)
        {

            var (estado, result, message) = await grupoRepository.UpdateGrupo(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> EliminarGrupo(int GrupoId)
        {

            var (estado, result, message) = await grupoRepository.DeleteGrupo(GrupoId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> GetGrupo(GrupoPayload payload)
        {

            var (estado, result, message) = await grupoRepository.GetGrupo(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    , message);

            return MessageResult<object>.Of(message, result);

        }


    }
}