using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class CreateProveedorPayload
    {
        public string Nombre { get; set; } = null!;
        public string? Dirección { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? UbigeoId { get; set; }
        public string? UsuarioCreacion { get; set; }

    }
}
