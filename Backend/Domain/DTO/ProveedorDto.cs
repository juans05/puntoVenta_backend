using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.DTO
{
    public class ProveedorDto
    {
        public int ProveedorId { get; set; }
        public int Index { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Dirección { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? UbigeoId { get; set; }
        public Ubigeo? Ubigeo { get; set; }
        public string? UsuarioCreacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Estado { get; set; }
    }
}
