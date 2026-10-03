namespace Domain.DTO;

public class CuentaContableDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public int? CuentaPadreId { get; set; }
    public bool Estado { get; set; }

    public int Nivel { get; set; }
    public string ClaseCuenta { get; set; } = null!;
    public bool TipoAnexo { get; set; }
    public string? TipoAnexoClase { get; set; }
    public bool CuentaMonetaria { get; set; }
    public bool AjusteDifCambio { get; set; }
    public string? CodigoEeff { get; set; }
    public string? CodigoEeffTributario { get; set; }
    public int? CodigoEeffNiifId { get; set; }
    public string? CodigoEeffNiif { get; set; }
    public string? ClasificacionBienServicio { get; set; }
    public bool Destino { get; set; }

    public string ModoCentroCosto { get; set; } = null!;
    public int? CuentaCargo1Id { get; set; }
    public string? CuentaCargo1 { get; set; }
    public int? CuentaAbono1Id { get; set; }
    public string? CuentaAbono1 { get; set; }
    public decimal? PorcentajeDestino1 { get; set; }
    public int? CuentaCargo2Id { get; set; }
    public string? CuentaCargo2 { get; set; }
    public int? CuentaAbono2Id { get; set; }
    public string? CuentaAbono2 { get; set; }
    public decimal? PorcentajeDestino2 { get; set; }
    public int? CuentaCargo3Id { get; set; }
    public string? CuentaCargo3 { get; set; }
    public int? CuentaAbono3Id { get; set; }
    public string? CuentaAbono3 { get; set; }
    public decimal? PorcentajeDestino3 { get; set; }
    public int? CuentaCierreId { get; set; }
    public string? CuentaCierre { get; set; }
}

public class CodigoEeffNiifDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Categoria { get; set; } = null!;
}
