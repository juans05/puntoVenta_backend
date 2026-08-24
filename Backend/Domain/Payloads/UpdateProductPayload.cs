using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class UpdateProductPayload
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = null!;
        public decimal Precio { get; set; }
        public int? CategoriaId { get; set; }
        public int? GrupoId { get; set; }
        public int? ProveedorId { get; set; }
        //public Categoria? Categoria { get; set; }
        //public Proveedor? Proveedor { get; set; }
        public string? CodigoBarra { get; set; }
        public decimal PrecioVentaSinInpuesto { get; set; }
        public decimal PrecioVentaConInpuesto { get; set; }
        public decimal MargenGanancia { get; set; }
        public bool CambioPrecioPermitido { get; set; }
        public int Stock { get; set; }
        public string? RutaImagen { get; set; }
        public string? Comentario { get; set; }
        public string? UsuarioModificacion { get; set; }
    }
}
