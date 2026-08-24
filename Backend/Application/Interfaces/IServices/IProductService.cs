using Domain.Models;
using Domain.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IProductService
    {
        Task<MessageResult<object>> CrearProducto(CreateProductPayload payload);
        Task<MessageResult<object>> ModificarProducto(UpdateProductPayload payload);
        Task<MessageResult<object>> ListarProductos(ProductPayload payload);
        Task<MessageResult<object>> EliminarProducto(int producto);
        Task<MessageResult<object>> PrevisualizarImportacion(ImportProductosPayload payload);
        Task<MessageResult<object>> ImportarProductos(ImportProductosPayload payload);
    }
}
