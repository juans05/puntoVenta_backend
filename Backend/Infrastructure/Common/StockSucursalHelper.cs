using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common;

// Centraliza el ajuste de stock por sucursal (ver Pieza "Transferencias entre sucursales"):
// mantiene ProductoSucursalStock (la sede real donde esta el stock) y Producto.Stock (el TOTAL
// agregado de todas las sedes, que sigue siendo lo que Dashboard/listados/disponibilidad en
// Ventas leen sin cambios) sincronizados en un solo lugar, en vez de repetir la logica en cada
// repositorio que hoy toca stock (Compra, OrdenCompra, Comprobante, Inventory, NotaCompra).
public static class StockSucursalHelper
{
    public class ResultadoAjuste
    {
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public int SucursalIdUsada { get; set; }
        public int StockAnteriorSucursal { get; set; }
        public int StockPosteriorSucursal { get; set; }
    }

    // sucursalId: la sede de la transaccion (Compra.SucursalId, ComprobanteCabecera.SucursalId,
    // etc.). Si viene null, cae en producto.SucursalId (el producto es propio de una sola sede) y,
    // si tampoco tiene, en la primera sucursal del tenant -- para no perder el numero en negocios
    // que todavia no separan sus productos por sede.
    public static async Task<ResultadoAjuste> Ajustar(SpaContext context, Producto producto, int? sucursalId, int delta)
    {
        var sucursalReal = sucursalId ?? producto.SucursalId ?? await ObtenerSucursalPorDefectoAsync(context);
        if (sucursalReal == null)
            return new ResultadoAjuste { Ok = false, Error = "No hay una sucursal registrada para asignar el movimiento de stock" };

        // IgnoreQueryFilters: la sede real puede no coincidir con la sede ambiental de la request
        // (ej. una Transferencia toca dos sedes a la vez), asi que el query filter por sucursal de
        // ProductoSucursalStock (ver SpaContext) se saltaria la fila que en verdad necesitamos.
        var fila = await context.ProductoSucursalStock.IgnoreQueryFilters().AsTracking()
            .FirstOrDefaultAsync(s => s.ProductoId == producto.Id && s.SucursalId == sucursalReal);

        if (fila == null)
        {
            // Backfill perezoso: si el producto nunca tuvo una fila en ninguna sede (viene de
            // antes de este rediseño, o se le puso Stock a mano como en los tests), su
            // Producto.Stock total se asume integro de la primera sede que lo toca -- asi no
            // hace falta una migracion de datos aparte para no perder el numero.
            var yaTracked = await context.ProductoSucursalStock.IgnoreQueryFilters()
                .AnyAsync(s => s.ProductoId == producto.Id);
            fila = new ProductoSucursalStock { ProductoId = producto.Id, SucursalId = sucursalReal.Value, Stock = yaTracked ? 0 : producto.Stock ?? 0 };
            context.ProductoSucursalStock.Add(fila);
        }

        var stockAnterior = fila.Stock;
        var stockNuevo = stockAnterior + delta;
        if (stockNuevo < 0)
            return new ResultadoAjuste { Ok = false, Error = $"Stock insuficiente en la sucursal para el producto {producto.Nombre}" };

        fila.Stock = stockNuevo;
        producto.Stock = Math.Max(0, (producto.Stock ?? 0) + delta);

        return new ResultadoAjuste
        {
            Ok = true,
            SucursalIdUsada = sucursalReal.Value,
            StockAnteriorSucursal = stockAnterior,
            StockPosteriorSucursal = stockNuevo
        };
    }

    private static async Task<int?> ObtenerSucursalPorDefectoAsync(SpaContext context) =>
        await context.Sucursal.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.TenantId == context.CurrentTenantName)
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
}
