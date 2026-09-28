using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

// Helper de partida doble reusado por OrdenCompraRepository (movimiento de inventario),
// CompraRepository (factura) y CuentasRepository (pago). Esos repos no conocen detalles de
// partida doble: solo piden generar/reversar con las cuentas y montos que les corresponden.
public interface IAsientoContableRepository
{
    Task<(ServiceStatus, AsientoContableDto?, string)> Generar(string origenTipo, int origenId, string glosa, List<LineaAsientoContable> lineas);
    Task<(ServiceStatus, AsientoContableDto?, string)> Reversar(string origenTipo, int origenId);
    // Copia las lineas del asiento activo de otro origen (invertido = Debe/Haber intercambiados)
    // bajo un origen NUEVO, sin tocar el asiento base -- para Notas de Credito/Debito, que ajustan
    // un documento existente pero son su propio movimiento contable, no una anulacion de aquel.
    Task<(ServiceStatus, AsientoContableDto?, string)> GenerarBasadoEn(string origenTipoBase, int origenIdBase, bool invertido, string nuevoOrigenTipo, int nuevoOrigenId, string glosa);
    Task<(ServiceStatus, List<AsientoContableDto>?, string)> Listar(DateTime? desde, DateTime? hasta, string? origenTipo);
    Task<(ServiceStatus, EstadoResultadosDto?, string)> ObtenerEstadoResultados(DateTime? desde, DateTime? hasta);
    Task<(ServiceStatus, BalanceGeneralDto?, string)> ObtenerBalanceGeneral(DateTime hasta);
}
