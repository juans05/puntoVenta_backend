namespace Domain.Entities;

// Equivalencia "como lo factura el proveedor" -> producto del catalogo. Se aprende al guardar una compra
// que vino de un XML (el usuario eligio el producto para esa linea) y permite registrar solas las
// siguientes facturas de ese proveedor (ver CompraRepository.RegistrarXmlSunat).
// Clave = codigo del proveedor (cac:SellersItemIdentification) o, sin codigo, su descripcion normalizada.
public class ProductoProveedor : EntityBase
{
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public string Clave { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
}
