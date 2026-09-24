namespace Domain.Entities;

// Una fila por tenant: decide si Compras usa el flujo ERP completo o el simplificado de siempre.
public class ConfiguracionFlujo : EntityBase
{
    public string FlujoCompras { get; set; } = FlujoComprasModo.Simplificado;
    public string CruceFactura { get; set; } = CruceFacturaModo.Advertir;
    // Ordenes con total mayor a este monto requieren aprobacion. Null = nunca se pide aprobacion.
    public decimal? MontoAprobacionOc { get; set; }
}

public static class FlujoComprasModo
{
    public const string Simplificado = "SIMPLIFICADO";
    public const string Completo = "COMPLETO";
}

public static class CruceFacturaModo
{
    public const string Advertir = "ADVERTIR";
    public const string Bloquear = "BLOQUEAR";
}
