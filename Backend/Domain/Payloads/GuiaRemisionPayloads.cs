namespace Domain.Payloads;

public class GuiaRemisionQueryParams
{
    public int Page { get; set; } = 1;
    public int Amount { get; set; } = 50;
}

// Datos de traslado editables tras generar la guia desde la entrega (cliente/direccion/placa ya
// vienen precargados del pedido y la entrega).
public class ActualizarGuiaRemisionPayload
{
    public DateTime? FechaTraslado { get; set; }
    public string? Motivo { get; set; }
    public string? ModTraslado { get; set; }
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
}
