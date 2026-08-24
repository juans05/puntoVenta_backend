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
    public class ProductService : IProductService
    {
        private readonly IProductRepository productRepository;
        private readonly IMapper mapper;

        public ProductService(IProductRepository productRepository, IMapper mapper)
        {
            this.productRepository = productRepository;
            this.mapper = mapper; 
        }

        public async Task<MessageResult<object>> CrearProducto(CreateProductPayload payload)
        {

            var (estado, result, message) = await productRepository.CreateProduct(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al crear", message);

            var mapeoProducto = mapper.Map<ProductoDto>(result);

            return MessageResult<object>.Of(message, mapeoProducto);

        }
        public async Task<MessageResult<object>> ModificarProducto(UpdateProductPayload payload)
        {

            var (estado, result, message) = await productRepository.UpdateProduct(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al Actualizar", message);

            var mapeoProducto = mapper.Map<ProductoDto>(result);

            return MessageResult<object>.Of(message, mapeoProducto);

        }
        public async Task<MessageResult<object>> EliminarProducto(int producto)
        {

            var (estado, result, message) = await productRepository.DeleteProduct(producto);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , "Error al Eliminar", message);

            return MessageResult<object>.Of(message, result);

        }
        public async Task<MessageResult<object>> ListarProductos(ProductPayload payload)
        {

            var (estado, result, message) = await productRepository.GetProducto(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                    ,  message, result);

            return MessageResult<object>.Of(message, result);

        }

        public async Task<MessageResult<object>> PrevisualizarImportacion(ImportProductosPayload payload)
        {

            var (estado, result, message) = await productRepository.PrevisualizarImportacion(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, result);

            return MessageResult<object>.Of(message, result);

        }

        public async Task<MessageResult<object>> ImportarProductos(ImportProductosPayload payload)
        {

            var (estado, result, message) = await productRepository.ImportarProductos(payload);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, result);

            return MessageResult<object>.Of(message, result);

        }


    }
}