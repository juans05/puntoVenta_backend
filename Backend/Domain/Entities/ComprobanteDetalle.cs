
namespace Domain.Entities
{
    public class ComprobanteDetalle : EntityBase
    {
        public int? SucursalId { get; set; }
        public int ComprobanteCabeceraId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }


        public decimal ValorUnitario { get; set; }
        public decimal ValorUnitarioTotal { get; set; }
        public decimal ValorIgv { get; set; }

        public decimal? Descuento { get; set; }

        // Costo real de esta linea (ej. lo que realmente costo el delivery de esta venta puntual),
        // distinto del costo de catalogo en Producto.CostoUnitario que puede no aplicar caso a caso.
        // Si es null, los reportes de margen usan Producto.CostoUnitario como antes.
        public decimal? CostoReal { get; set; }


        //public decimal? PrecioFicha { get; set; }
        //public decimal? PrecioFichaSinAumento { get; set; }

        public ComprobanteCabecera ComprobanteCabecera { get; set; } = null!;
        public Producto Producto { get; set; } = null!;
    }
}
