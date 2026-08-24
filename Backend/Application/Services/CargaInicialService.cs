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
    public class CargaInicialService : ICargaInicialService
    {
        private readonly ICargaInicialRepository cargaInicialRepository;

        public CargaInicialService(ICargaInicialRepository cargaInicialRepository)
        {
            this.cargaInicialRepository = cargaInicialRepository;
        }

        public async Task<MessageResult<object>> CrearDataInicialRubro(int RubroId, string TenantId)
        {

            var (estado, result, message) = await cargaInicialRepository.CrearDataInicialSegunRubro(RubroId, TenantId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> GetCategoriaRubro(int RubroId, string TenantId)
        {

            var (estado, result, message) = await cargaInicialRepository.GetCategoriasRubro(RubroId, TenantId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
    }
}
