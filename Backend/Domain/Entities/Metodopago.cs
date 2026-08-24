namespace Domain.Entities
{
    public class Metodopago : EntityBase
    {
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int? Posicion { get; set; } //orden
        public bool? PagoRapido { get; set; }
        public bool? CambioPermitido { get; set; }
        public bool? MarcarTransaccionPagada { get; set; }
        public bool? ImprimirRecibo { get; set; }
        public string? TeclaAccesoDirecto { get; set; }
        public List<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
