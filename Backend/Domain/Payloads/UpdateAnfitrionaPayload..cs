using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class UpdateAnfitrionaPayload
    {
        public int AnfitrionaId { get; set; }
        public string Nombres { get; set; } = null!;
        public string? Apellidos { get; set; }
        public int NacionalidadId { get; set; }
        public string? Direccion { get; set; }
        public string? Celular { get; set; }
        public string? Foto { get; set; }
        public string? UsuarioModificacion { get; set; }
    }
}
