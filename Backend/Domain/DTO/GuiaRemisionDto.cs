namespace Domain.DTO;

public class GuiaRemisionDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public int EntregaId { get; set; }
    public string? EntregaNumero { get; set; }
    public int PedidoVentaId { get; set; }
    public string? PedidoVentaNumero { get; set; }
    public string FechaEmision { get; set; } = null!;
    public string FechaTraslado { get; set; } = null!;
    public string? ClienteNombre { get; set; }
    public string? ClienteDocumento { get; set; }
    public string Motivo { get; set; } = null!;
    public string ModTraslado { get; set; } = null!;
    public decimal? PesoTotal { get; set; }
    public string? UndPesoTotal { get; set; }
    public string? UbigeoPartida { get; set; }
    public string? DireccionPartida { get; set; }
    public string? UbigeoLlegada { get; set; }
    public string? DireccionLlegada { get; set; }
    public string? TransportistaRuc { get; set; }
    public string? TransportistaRazonSocial { get; set; }
    public string? TransportistaMtc { get; set; }
    public string? ChoferNombre { get; set; }
    public string? ChoferDocumento { get; set; }
    public string? Placa { get; set; }
    public string EstadoGuia { get; set; } = null!;
    public List<GuiaRemisionDetalleDto> Detalle { get; set; } = new();
}

public class GuiaRemisionDetalleDto
{
    public int ProductoId { get; set; }
    public string? Producto { get; set; }
    public int Cantidad { get; set; }
    public string? Unidad { get; set; }
}
