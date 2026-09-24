namespace Domain.Entities;

// Guia de remision remitente: documento INTERNO de traslado generado desde una Entrega.
// No se envia a SUNAT (sin proxy, credenciales, ticket ni estado de SUNAT).
public class GuiaRemision : EntityBase
{
    public int? SucursalId { get; set; }
    public string Numero { get; set; } = null!;
    public int EntregaId { get; set; }
    public Entrega? Entrega { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime FechaTraslado { get; set; }

    // Snapshot del destinatario al momento de generar (el cliente del pedido puede cambiar despues).
    public string? ClienteNombre { get; set; }
    public string? ClienteDocumento { get; set; }

    public string Motivo { get; set; } = MotivoTraslado.Venta;
    public string ModTraslado { get; set; } = ModalidadTraslado.Publico;
    public decimal? PesoTotal { get; set; }
    public string? UndPesoTotal { get; set; }

    public string? UbigeoPartida { get; set; }
    public string? DireccionPartida { get; set; }
    public string? UbigeoLlegada { get; set; }
    public string? DireccionLlegada { get; set; }

    // Transporte publico.
    public string? TransportistaRuc { get; set; }
    public string? TransportistaRazonSocial { get; set; }
    public string? TransportistaMtc { get; set; }

    // Transporte privado.
    public string? ChoferNombre { get; set; }
    public string? ChoferDocumento { get; set; }
    public string? Placa { get; set; }

    public string EstadoGuia { get; set; } = EstadoGuiaRemision.Emitida;
    public List<GuiaRemisionDetalle> Detalles { get; set; } = new();
}

public class GuiaRemisionDetalle : EntityBase
{
    public int? SucursalId { get; set; }
    public int GuiaRemisionId { get; set; }
    public GuiaRemision? GuiaRemision { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public string? Unidad { get; set; }
}

public static class EstadoGuiaRemision
{
    public const string Emitida = "EMITIDA";
    public const string Anulada = "ANULADA";
}

public static class MotivoTraslado
{
    public const string Venta = "01";
}

public static class ModalidadTraslado
{
    public const string Publico = "01";
    public const string Privado = "02";
}
