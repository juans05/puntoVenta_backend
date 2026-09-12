using Domain.DTO;

namespace Domain.Payloads;

public class CreatePedidoPayload
{
    public int SucursalId { get; set; }
    public decimal Total { get; set; }
    public int? ClienteId { get; set; }
    public string? Nombre { get; set; }
    public string? Dni { get; set; }
    public string? Celular { get; set; }
    public List<CreatePedidoDetallePayload> Detalles { get; set; } = new();
}

public class CreatePedidoDetallePayload
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }
}

public class UpdatePedidoEstadoPayload
{
    public int Id { get; set; }
    public char EstadoPedido { get; set; }
    public string? CodigoSeguimiento { get; set; }
}

public class PedidoQueryParams : PaginationPayload
{
    public char? EstadoPedido { get; set; }
}

public class PedidoPublicoSubmitPayload
{
    public string Nombre { get; set; } = null!;
    public string Dni { get; set; } = null!;
    public string Celular { get; set; } = null!;
    public string UbigeoId { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? Referencia { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? Currier { get; set; }
    public int? SalonId { get; set; }
}

public class ClienteLoginPayload
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class ReenviarPasswordPayload
{
    public int PedidoId { get; set; }
}

public class EtiquetaEnvioDTO
{
    public int PedidoId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public string Distrito { get; set; } = null!;
    public string Celular { get; set; } = null!;
    public string TipoEnvio { get; set; } = null!;
    public string? CodigoSeguimiento { get; set; }
}