using Domain.DTO;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepository
{
    public interface ICargaInicialRepository
    {
        Task<(ServiceStatus, CategoriaDto?, string)> CrearDataInicialSegunRubro(int RubroId, string TenantId);
        Task<(ServiceStatus, List<CategoriaDto>?, string)> GetCategoriasRubro(int RubroId, string TenantId);
    }
}
