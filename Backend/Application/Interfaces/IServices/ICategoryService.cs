using Domain.Models;
using Domain.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface ICategoryService
    {
        Task<MessageResult<object>> CrearCategoria(CreateCategoryPayload payload);
        Task<MessageResult<object>> GetCategoria(CategoryPayload payload);
        Task<MessageResult<object>> ModificarCategoria(UpdateCategoryPayload payload);
        Task<MessageResult<object>> EliminarCategoria(int CategoriaId);
    }
}
