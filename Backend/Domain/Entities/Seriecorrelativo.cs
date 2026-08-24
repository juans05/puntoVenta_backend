
namespace Domain.Entities
{
    public class Seriecorrelativo : EntityBase
    {
        public int? SucursalId { get; set; }
        public int TipoDocumentoVentaId { get; set; }
        public string Serie { get; set; } = null!;
        public int Correlativo { get; set; }
        public TipoDocumentoVenta TipoDocumentoVenta { get; set; } = null!;
    }
}
