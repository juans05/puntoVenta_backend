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

        // Es un servicio (sin inventario: sin stock/lotes/presentaciones/peso) en vez de un bien
        // fisico. Basado en el toggle "Inventory Item" de SAP Item Master Data, simplificado a un
        // solo campo -- ver ProductoModal (frontend) para que campos se ocultan segun este valor.
        public bool EsServicio { get; set; }

        // Toggles estilo QuickBooks "I sell this product/service" / "I purchase this product/service":
        // controlan si el producto aparece como opcion al armar una Factura (Ventas) o una Compra.
        // Default true para no ocultar productos existentes al desplegar esta columna.
        public bool SeVende { get; set; } = true;
        public bool SeCompra { get; set; } = true;

        // Cuentas contables del producto (tab "Contabilidad" del modal), todas opcionales: si no se
        // eligen, ComprobanteRepository.CrearComprobante usa las cuentas PCGE por defecto (70 Ventas,
        // 20 Mercaderias, 69 Costo de Ventas) al generar el asiento de la venta.
        public int? CuentaIngresoId { get; set; }
        public CuentaContable? CuentaIngreso { get; set; }
        public int? CuentaInventarioId { get; set; }
        public CuentaContable? CuentaInventario { get; set; }
        public int? CuentaCostoId { get; set; }
        public CuentaContable? CuentaCosto { get; set; }
        // Pares Debe/Haber obligatorios en la UI: ingreso = Debe (CuentaIngresoDebeId) / Haber (CuentaIngresoId);
        // gasto = Debe (CuentaCostoId) / Haber (CuentaGastoHaberId). Solo cuentas de ultimo nivel (8 digitos).
        public int? CuentaIngresoDebeId { get; set; }
        public CuentaContable? CuentaIngresoDebe { get; set; }
        public int? CuentaGastoHaberId { get; set; }
        public CuentaContable? CuentaGastoHaber { get; set; }
        // JSON {"<TipoMovimientoInventario>": cuentaId}: cuenta de inventario por tipo de movimiento
        // (ponytail: sin FK por entrada; pasar a tabla hija si hace falta integridad/reportes).
        public string? CuentasInventarioMovimiento { get; set; }

        public List<PrecioAlternativo> PreciosAlternativos { get; set; } = new List<PrecioAlternativo>();
        public List<Presentacion> Presentaciones { get; set; } = new List<Presentacion>();
        public List<ComprobanteDetalle> ComprobanteDetalles { get; set; } = new List<ComprobanteDetalle>();



    }
}
