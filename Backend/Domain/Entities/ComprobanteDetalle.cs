
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


        //public decimal? PrecioFicha { get; set; }
        //public decimal? PrecioFichaSinAumento { get; set; }

        public ComprobanteCabecera ComprobanteCabecera { get; set; } = null!;
        public Producto Producto { get; set; } = null!;
    }
}
