using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class CreateComentarioPayload
    {
        public int Item { get; set; }
        public string Descripcion { get; set; } = null!;
        //public int ProductoId { get; set; }
        //public string? UsuarioCreacion { get; set; }

    }
}
