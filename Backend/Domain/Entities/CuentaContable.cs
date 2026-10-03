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

// Niveles de la jerarquia PCGE (ver "configuracion plan de cuentas ERPdocx.docx"): el codigo
// decide el nivel por su cantidad de digitos -- 2/3/4/5/8 -- no se elige a mano. Solo Nivel 5
// (8 digitos) es la hoja que admite asientos; el motor de asientos automaticos (Compras/Ventas)
// hoy postea contra cuentas cortas (ej. "20", "42") y eso NO se tocó en este cambio -- ver nota
// en CuentaContableRepository.
public static class ModoCentroCostoCuenta
{
    public const string Ninguno = "NINGUNO";
    public const string Opcional = "OPCIONAL";
    public const string Obligatorio = "OBLIGATORIO";
}

public class CuentaContable : EntityBase
{
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;

    public int? CuentaPadreId { get; set; }
    public CuentaContable? CuentaPadre { get; set; }

    // Nivel y ClaseCuenta se calculan del Codigo al guardar (CuentaContableRepository.AplicarCampos),
    // no se reciben del payload -- "deben guardar absoluta congruencia con la estructura numerica".
    public int Nivel { get; set; }
    public string ClaseCuenta { get; set; } = null!;
    // Si TipoAnexo=true, a que tipo de tercero se vincula (Cliente/Proveedor/Empleado/...).
    public bool TipoAnexo { get; set; }
    public string? TipoAnexoClase { get; set; }
    public bool CuentaMonetaria { get; set; }
    public bool AjusteDifCambio { get; set; }
    public string? CodigoEeff { get; set; }
    public string? CodigoEeffTributario { get; set; }
    public int? CodigoEeffNiifId { get; set; }
    public CodigoEeffNiif? CodigoEeffNiif { get; set; }
    public string? ClasificacionBienServicio { get; set; }
    public bool Destino { get; set; }

    // No aplica/Opcional/Obligatorio -- si es Obligatorio, cada linea de compra que postee a esta
    // cuenta debe traer CentroCostoId (ver CompraRepository.ValidarCentroCostoObligatorio).
    public string ModoCentroCosto { get; set; } = ModoCentroCostoCuenta.Ninguno;

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
