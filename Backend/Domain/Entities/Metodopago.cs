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
        // Cuenta de ultimo nivel (caja o banco, ej. 1041xxxx) que mueve este medio en el asiento de
        // cobros y pagos (CuentasRepository.RegistrarPago). Sin elegir, va a la "10" por defecto.
        public int? CuentaContableId { get; set; }
        public CuentaContable? CuentaContable { get; set; }
        public List<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
