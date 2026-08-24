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
    public interface IGrupoRepository
    {
        Task<(ServiceStatus, GrupoDto?, string)> CrearGrupo(CreateGrupoPayload payload);
        Task<(ServiceStatus, List<GrupoDto>?, string)> GetGrupo(GrupoPayload payload);
        Task<(ServiceStatus, GrupoDto?, string)> UpdateGrupo(UpdateGrupoPayload payload);
        Task<(ServiceStatus, Grupo?, string)> DeleteGrupo(int GrupoId);
    }
}
