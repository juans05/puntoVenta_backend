using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface ICargaInicialService
    {

        Task<MessageResult<object>> CrearDataInicialRubro(int RubroId, string TenantId);
        Task<MessageResult<object>> GetCategoriaRubro(int RubroId, string TenantId);
    }
}
