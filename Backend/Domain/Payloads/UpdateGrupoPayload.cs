using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class UpdateGrupoPayload
    {
        public int GrupoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? UsuarioMofificacion { get; set; }
    }
}
