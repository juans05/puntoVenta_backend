namespace Domain.Entities
{
    public class Proveedor : EntityBase
    {
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = null!;
        public int? TipoDocumentoId { get; set; }
        public TipoDocumento? TipoDocumento { get; set; }
        public string? Ruc { get; set; }
        public string? Dirección { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? UbigeoId { get; set; }
        public Ubigeo? Ubigeo { get; set; }
        public string? DetalleAdicional { get; set; }

        // Cuenta por Pagar propia del proveedor: si no se elige, la compra a credito postea contra
        // "42" (ver CompraRepository) -- mismo patron que Producto.CuentaIngresoId.
        public int? CuentaPorPagarId { get; set; }
        public CuentaContable? CuentaPorPagar { get; set; }

        public List<Producto> Productos { get; set; } = new List<Producto>();

    }
}
