using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepository
{
    public interface IProductRepository
    {
        Task<(ServiceStatus, Producto?, string)> CreateProduct(CreateProductPayload payload);
        Task<(ServiceStatus, Producto?, string)> UpdateProduct(UpdateProductPayload payload);
        Task<(ServiceStatus, Producto?, string)> DeleteProduct(int ProductoId);

        Task<(ServiceStatus, DataCollection<ProductoDto>?, string)> GetProducto(ProductPayload payload);

        Task<(ServiceStatus, PreviewImportDto?, string)> PrevisualizarImportacion(ImportProductosPayload payload);

        Task<(ServiceStatus, int, string)> ImportarProductos(ImportProductosPayload payload);
    }
}
