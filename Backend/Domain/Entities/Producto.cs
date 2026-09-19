namespace Domain.Entities
{
public class Producto : EntityBase
    {
        public string Nombre { get; set; } = null!;

        public int? SucursalId { get; set; }

          
        public decimal Precio { get; set; }
        public int? CategoriaId { get; set; }
        public Categoria? Categoria { get; set; } = null!;
        public int? GrupoId { get; set; }
        public Grupo? Grupo { get; set; } = null!;
        /*Nuevos campos*/
        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }
        public string? CodigoBarra { get; set; }

         
        public decimal? PrecioVentaSinInpuesto { get; set; }

         
        public decimal? PrecioVentaConInpuesto { get; set; }

         
        public decimal? MargenGanancia { get; set; }
        // Costo de la última compra registrada (CompraDetalle.CostoUnitario). Base real
        // para calcular costo de ventas / utilidad; antes el Dashboard usaba Precio (venta).
        public decimal? CostoUnitario { get; set; }
        public bool? CambioPrecioPermitido { get; set; }
        public int? Stock { get; set; }
        public int? StockMinimo { get; set; }
        public int RestriccionEdad { get; set; }
        public string? Descripcion { get; set; }
        public string? RutaImagen { get; set; }
        public string? CloudinaryPublicId { get; set; }
        public string? Comentario { get; set; }

        public string? Codigo { get; set; }
        public string? Marca { get; set; }
        public int? MonedaId { get; set; }
        public Moneda? Moneda { get; set; }
        public int? TipoIgvId { get; set; }
        public TipoIgv? TipoIgv { get; set; }
        public int? UnidadMedidaId { get; set; }
        public UnidadMedida? UnidadMedida { get; set; }
        public decimal? PrecioMinimo { get; set; }
        public decimal? PesoKg { get; set; }
        public bool Icbper { get; set; }
        public decimal? PorcentajeDetraccion { get; set; }
        public string? DestinoPreparacion { get; set; }
        public bool GestionLotes { get; set; }
        public bool MultiPrecioActivo { get; set; }

        public List<PrecioAlternativo> PreciosAlternativos { get; set; } = new List<PrecioAlternativo>();
        public List<Presentacion> Presentaciones { get; set; } = new List<Presentacion>();
        public List<ComprobanteDetalle> ComprobanteDetalles { get; set; } = new List<ComprobanteDetalle>();



    }
}
