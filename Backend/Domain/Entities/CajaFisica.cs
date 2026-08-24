namespace Domain.Entities;

public class CajaFisica : EntityBase // Catálogo de cajas del local (Terminal/Caja): puede haber varias por sucursal.
{
    public string Nombre { get; set; } = null!;
    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }
}