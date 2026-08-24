using Domain.Models;
using Domain.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IGrupoService
    {
        Task<MessageResult<object>> CrearGrupo(CreateGrupoPayload payload);
        Task<MessageResult<object>> GetGrupo(GrupoPayload payload);
        Task<MessageResult<object>> ModificarGrupo(UpdateGrupoPayload payload);
        Task<MessageResult<object>> EliminarGrupo(int GrupoId);
    }
}
