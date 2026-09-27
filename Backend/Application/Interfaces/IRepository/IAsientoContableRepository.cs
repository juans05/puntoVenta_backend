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
    Task<(ServiceStatus, List<AsientoContableDto>?, string)> Listar(DateTime? desde, DateTime? hasta, string? origenTipo);
}
