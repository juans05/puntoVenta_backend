namespace Domain.Entities;

public class TipoIgv : EntityBase
{
    public string Codigo { get; set; } = null!;       // "10","20","30" (SUNAT tabla 07)
    public string Descripcion { get; set; } = null!;
    public bool AplicaPorcentajeImpuesto { get; set; } // true=Gravado, false=Exonerado/Inafecto (0%)
    // % propio del tipo (18, 10.5, 11...). null = el % del negocio (ConfiguracionFiscal); las filas base de SUNAT lo dejan en null.
    public decimal? Porcentaje { get; set; }

    public decimal Tasa(decimal tasaNegocio) => !AplicaPorcentajeImpuesto ? 0m : Porcentaje ?? tasaNegocio;
}

// Cuenta contable donde va el IGV de un tipo de afectacion, por negocio (TenantId). Va aparte de
// TipoIgv porque las filas base de SUNAT son compartidas por todos los negocios y cada uno tiene
// su propio plan de cuentas. Sin fila aqui, los asientos usan 40111.
public class TipoIgvCuenta : EntityBase
{
    public int TipoIgvId { get; set; }
    public TipoIgv? TipoIgv { get; set; }
    public int CuentaContableId { get; set; }
    public CuentaContable? CuentaContable { get; set; }
}
