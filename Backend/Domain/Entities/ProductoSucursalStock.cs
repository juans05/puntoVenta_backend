namespace Domain.Entities;

// Stock por sucursal de un producto. Producto.Stock sigue existiendo como el TOTAL agregado de
// todas las sedes (mantenido en sincronia por Infrastructure.Common.StockSucursalHelper) -- todo
// el codigo que ya lee Producto.Stock (Dashboard, listados, disponibilidad en Ventas) sigue
// funcionando igual, solo que ahora representa la suma de todas las sucursales en vez de ser el
// unico numero que existia antes de esta pieza.
public class ProductoSucursalStock : EntityBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }
    public int Stock { get; set; }
}
