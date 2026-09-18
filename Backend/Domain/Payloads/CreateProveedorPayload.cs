using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class CreateProveedorPayload
    {
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = null!;
        public int? TipoDocumentoId { get; set; }
        public string? Ruc { get; set; }
        public string? Dirección { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? UbigeoId { get; set; }
        public string? DetalleAdicional { get; set; }
        public string? UsuarioCreacion { get; set; }

    }
}
