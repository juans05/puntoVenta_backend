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
    public interface ICategoryRepository
    {
        Task<(ServiceStatus, CategoriaDto?, string)> CrearCategoria(CreateCategoryPayload payload);
        Task<(ServiceStatus, CategoriaDto?, string)> UpdateCategory(UpdateCategoryPayload payload);
        Task<(ServiceStatus, List<CategoriaDto>?, string)> GetCategoria(CategoryPayload payload);
        Task<(ServiceStatus, Producto?, string)> DeleteCategory(int CategoriaId);
    }
}
