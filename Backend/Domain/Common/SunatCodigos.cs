namespace Domain.Common;

// Codigos de las tablas oficiales de SUNAT usadas por el PLE (Programa de Libros Electronicos):
// Tabla 10 (tipo de comprobante), Tabla 2 (tipo de documento de identidad), Tabla 17 (estado de la operacion).
public static class SunatCodigos
{
    // TipoDocumentoVentaId interno -> Tabla 10. Solo cubre los documentos fiscales reales
    // (Factura/Boleta/NC/ND); Nota de Venta y Cotizacion no son comprobantes SUNAT y no entran
    // al libro electronico.
    public static string TipoComprobante(int tipoDocumentoVentaId) => tipoDocumentoVentaId switch
    {
        1 => "01", // Factura
        2 => "03", // Boleta de venta
        4 => "07", // Nota de credito
        5 => "08", // Nota de debito
        _ => ""
    };

    // TipoDocumentoId interno (tipodocumento.json) -> Tabla 2.
    public static string TipoDocumentoIdentidad(int? tipoDocumentoId) => tipoDocumentoId switch
    {
        1 => "1", // DNI
        2 => "7", // Pasaporte
        3 => "4", // Carnet de Extranjeria
        5 => "6", // RUC
        _ => "0"  // Doc.Trib.No.Dom.Sin.RUC / no identificado
    };

    // Proveedor no tiene TipoDocumentoId propio (solo un Ruc de texto libre) -- se infiere por
    // longitud, igual que ya hace el resto del sistema al crear un Proveedor desde un XML.
    public static string TipoDocumentoIdentidadPorNumero(string? numero) => (numero?.Trim().Length) switch
    {
        11 => "6", // RUC
        8 => "1",  // DNI
        _ => "0"
    };

    // EstadoComprobante ('C'/'F'/'A', ver EstatusComprobante) -> Tabla 17.
    public static string Estado(char estadoComprobante) => estadoComprobante == 'A' ? "9" : "1";
}
