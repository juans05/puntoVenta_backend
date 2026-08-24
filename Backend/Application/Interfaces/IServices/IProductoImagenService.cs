using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IProductoImagenService
{
    Task<MessageResult<object>> SubirImagen(ProductoImagenPayload payload);
    Task<MessageResult<object>> EliminarImagen(int productoId);
}