namespace Domain.DTO;

public class PedidoDTO
{
    public int Index { get; set; }
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public char EstadoPedido { get; set; }
    public string EstadoPedidoDescripcion { get; set; } = null!;
    public decimal Total { get; set; }
    public int? ClienteId { get; set; }
    public string? Nombre { get; set; }
    public string? Dni { get; set; }
    public string? Celular { get; set; }
    public string? UbigeoId { get; set; }
    public string? UbigeoNombre { get; set; }
    public string? TipoEnvio { get; set; }
    public string? Direccion { get; set; }
    public string? Referencia { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? Currier { get; set; }
    public int? SalonId { get; set; }
    public string? SalonNombre { get; set; }
    public string? CodigoSeguimiento { get; set; }
    public DateTime? FechaDespacho { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public bool PasswordEnviado { get; set; }
    public int? ComprobanteCabeceraId { get; set; }
    public string? FechaCreacion { get; set; }
    public string? UsuarioCreacion { get; set; }

    public List<PedidoDetalleDTO> PedidoDetalles { get; set; } = new List<PedidoDetalleDTO>();
}

public class PedidoDetalleDTO
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = null!;
    public string? ProductoImagen { get; set; }
    public int Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Subtotal => Cantidad * ValorUnitario;
}

public class PedidoPublicoDTO
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public decimal Total { get; set; }
    public char EstadoPedido { get; set; }
    public List<PedidoDetallePublicoDTO> PedidoDetalles { get; set; } = new List<PedidoDetallePublicoDTO>();
}

public class PedidoDetallePublicoDTO
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = null!;
    public string? ProductoImagen { get; set; }
    public int Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
}