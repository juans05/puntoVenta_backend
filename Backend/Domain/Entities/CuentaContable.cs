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

    // Metadata adicional de la cuenta (formato tipo SAP B1 / EEFF Peru) -- solo se guarda, ningun
    // asiento automatico del sistema los usa todavia.
    public int? Nivel { get; set; }
    // "Desglosable" en el catalogo de referencia (PCGE.xlsx): son codigos SUNAT/EEFF sin un
    // catalogo propio en este sistema todavia, se guardan como texto libre.
    public string? ClaseCuenta { get; set; }
    public bool TipoAnexo { get; set; }
    public bool CuentaMonetaria { get; set; }
    public bool AjusteDifCambio { get; set; }
    public string? CodigoEeff { get; set; }
    public string? CodigoEeffTributario { get; set; }
    public string? CodigoEeffNiif { get; set; }
    public string? ClasificacionBienServicio { get; set; }
    public bool Destino { get; set; }

    // "Seleccion de cuenta contable" / catalogo de Centro de Costo: estos si son selects reales.
    public int? CentroCostoId { get; set; }
    public CentroCosto? CentroCosto { get; set; }
    public int? CuentaCargo1Id { get; set; }
    public CuentaContable? CuentaCargo1 { get; set; }
    public int? CuentaAbono1Id { get; set; }
    public CuentaContable? CuentaAbono1 { get; set; }
    public decimal? PorcentajeDestino1 { get; set; }
    public int? CuentaCargo2Id { get; set; }
    public CuentaContable? CuentaCargo2 { get; set; }
    public int? CuentaAbono2Id { get; set; }
    public CuentaContable? CuentaAbono2 { get; set; }
    public decimal? PorcentajeDestino2 { get; set; }
    public int? CuentaCargo3Id { get; set; }
    public CuentaContable? CuentaCargo3 { get; set; }
    public int? CuentaAbono3Id { get; set; }
    public CuentaContable? CuentaAbono3 { get; set; }
    public decimal? PorcentajeDestino3 { get; set; }
    public int? CuentaCierreId { get; set; }
    public CuentaContable? CuentaCierre { get; set; }
}
