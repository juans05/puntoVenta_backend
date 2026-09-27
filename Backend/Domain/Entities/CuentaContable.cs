namespace Domain.Entities;

// Plan de cuentas (PCGE) editable por tenant. Sembrado inicial desde
// Infrastructure/Data/Default/cuentacontable.json (ver PuntoVentaDbContextData.SeedCatalogoTenant);
// el usuario puede agregar, editar o desactivar cuentas despues.
public static class TipoCuentaContable
{
    public const string Activo = "Activo";
    public const string Pasivo = "Pasivo";
    public const string Patrimonio = "Patrimonio";
    public const string Ingreso = "Ingreso";
    public const string Gasto = "Gasto";
}

public class CuentaContable : EntityBase
{
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;

    public int? CuentaPadreId { get; set; }
    public CuentaContable? CuentaPadre { get; set; }
}
