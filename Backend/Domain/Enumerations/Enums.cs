namespace Domain.Enumerations;

public enum TipoPago
{
    Efectivo = 1,
    Tarjeta,
    Yape,
    Transferencia
}

public enum TipoComprobante
{
    Factura = 1,
    Boleta,
    TicketInterno
}
public enum CategoriaEnum
{
    Cortes = 1,
    Tintes = 2,
    Otros = 0
}

public static class EstatusComprobante
{
    public const char Creado = 'C';
    public const char Facturado = 'F';
    public const char Anulado = 'A';
}

public static class EstatusEnvioSunat
{
    public const char Pendiente = 'P';
    public const char Enviado = 'E';
    public const char Error = 'X';
}

public enum TipoMovimientoInventario
{
    Compra = 1,
    Venta = 2,
    AjusteEntrada = 3,
    AjusteSalida = 4,
    DevolucionCompra = 5,
    DevolucionVenta = 6
}

