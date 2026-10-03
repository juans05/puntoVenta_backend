
namespace Domain.Entities;

public class Cliente : EntityBase
{
    public int? SucursalId { get; set; }
    public string? Nombre { get; set; }
    public int? TipoDocumentoId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Sexo { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? UbigeoId { get; set; }
    public Ubigeo? Ubigeo { get; set; }

    // Cuenta por Cobrar propia del cliente: si no se elige, la venta a credito postea contra "12"
    // (ver ComprobanteRepository.GenerarAsientoVenta) -- mismo patron que Producto.CuentaIngresoId.
    public int? CuentaPorCobrarId { get; set; }
    public CuentaContable? CuentaPorCobrar { get; set; }

    public TipoDocumento? TipoDocumento { get; set; }
    public List<ComprobanteCabecera> ComprobanteCabeceras { get; set; } = new List<ComprobanteCabecera>();
}
