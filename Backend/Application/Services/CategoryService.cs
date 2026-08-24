using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
        }

        public async Task<MessageResult<object>> CrearCategoria(CreateCategoryPayload payload)
        {

            var (estado, result, message) = await categoryRepository.CrearCategoria(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> ModificarCategoria(UpdateCategoryPayload payload)
        {

            var (estado, result, message) = await categoryRepository.UpdateCategory(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> EliminarCategoria(int CategoriaId)
        {

            var (estado, result, message) = await categoryRepository.DeleteCategory(CategoriaId);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> GetCategoria(CategoryPayload payload)
        {

            var (estado, result, message) = await categoryRepository.GetCategoria(payload);

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