using Domain.Enumerations;

namespace Domain.Entities;

public class Pedido : EntityBase
{
    public int? SucursalId { get; set; }
    public string Token { get; set; } = null!;
    public char EstadoPedido { get; set; }
    public decimal Total { get; set; }

    public int? ClienteId { get; set; }
    public string? Nombre { get; set; }
    public string? Dni { get; set; }
    public string? Celular { get; set; }

    public string? UbigeoId { get; set; }
    public string? TipoEnvio { get; set; }
    public string? Direccion { get; set; }
    public string? Referencia { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    public string? CodigoSeguimiento { get; set; }
    public DateTime? FechaDespacho { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public bool PasswordEnviado { get; set; }

    public int? ComprobanteCabeceraId { get; set; }

    public Cliente? Cliente { get; set; }
    public Ubigeo? Ubigeo { get; set; }
    public ComprobanteCabecera? ComprobanteCabecera { get; set; }
    public List<PedidoDetalle> PedidoDetalles { get; set; } = new();
}

public class PedidoDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal ValorUnitario { get; set; }

    public Pedido Pedido { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}

public static class EstatusPedido
{
    public const char Enviado = 'E';
    public const char DatosCompletos = 'D';
    public const char EnPreparacion = 'P';
    public const char Despachado = 'S';
    public const char EnCamino = 'C';
    public const char Entregado = 'T';
    public const char Cancelado = 'A';
}