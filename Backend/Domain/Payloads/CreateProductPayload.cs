using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class CreateProductPayload
    {
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
        public int? StockMinimo { get; set; }
        public string? RutaImagen { get; set; }
        public string? Comentario { get; set; }
        public string? UsuarioCreacion { get; set; }

        public int? SucursalId { get; set; }
        public string? Codigo { get; set; }
        public string? Marca { get; set; }
        public int? MonedaId { get; set; }
        public int? TipoIgvId { get; set; }
        public int? UnidadMedidaId { get; set; }
        public decimal? PrecioMinimo { get; set; }
        public decimal? PesoKg { get; set; }
        public bool Icbper { get; set; }
        public decimal? PorcentajeDetraccion { get; set; }
        public string? DestinoPreparacion { get; set; }
        public bool GestionLotes { get; set; }
        public bool MultiPrecioActivo { get; set; }
        public List<PrecioAlternativoPayload>? PreciosAlternativos { get; set; }
        public List<PresentacionPayload>? Presentaciones { get; set; }
    }
}
