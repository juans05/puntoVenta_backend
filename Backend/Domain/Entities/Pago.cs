
namespace Domain.Entities
{
    public class Pago : EntityBase
    {
        public int? SucursalId { get; set; }
        public int ComprobanteCabeceraId { get; set; }
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
        public int? CajaId { get; set; }

        public ComprobanteCabecera ComprobanteCabecera { get; set; } = null!;
        public Caja? Caja { get; set; }
        public Metodopago Metodopago { get; set; } = null!;
    }
}
