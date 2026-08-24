using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class GrupoDto
    {
        public int CategoriaId { get; set; }
        public int GrupoId { get; set; }
        public int Index { get; set; }
        public string Nombre { get; set; } = null!;
        public string? UsuarioCreacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Estado { get; set; }
    }
}
