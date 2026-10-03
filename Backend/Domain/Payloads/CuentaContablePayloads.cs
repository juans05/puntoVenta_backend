namespace Domain.Payloads;

public class CrearCuentaContablePayload
{
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public int? CuentaPadreId { get; set; }

    // Nivel y ClaseCuenta NO se reciben aca: se calculan del Codigo (ver
    // CuentaContableRepository.AplicarCampos).
    public bool TipoAnexo { get; set; }
    public string? TipoAnexoClase { get; set; }
    public bool CuentaMonetaria { get; set; }
    public bool AjusteDifCambio { get; set; }
    public string? CodigoEeff { get; set; }
    public string? CodigoEeffTributario { get; set; }
    public int? CodigoEeffNiifId { get; set; }
    public string? ClasificacionBienServicio { get; set; }
    public bool Destino { get; set; }

    // NINGUNO (default) / OPCIONAL / OBLIGATORIO -- ver ModoCentroCostoCuenta.
    public string? ModoCentroCosto { get; set; }
    public int? CuentaCargo1Id { get; set; }
    public int? CuentaAbono1Id { get; set; }
    public decimal? PorcentajeDestino1 { get; set; }
    public int? CuentaCargo2Id { get; set; }
    public int? CuentaAbono2Id { get; set; }
    public decimal? PorcentajeDestino2 { get; set; }
    public int? CuentaCargo3Id { get; set; }
    public int? CuentaAbono3Id { get; set; }
    public decimal? PorcentajeDestino3 { get; set; }
    public int? CuentaCierreId { get; set; }
}

public class ActualizarCuentaContablePayload : CrearCuentaContablePayload
{
    public bool Estado { get; set; } = true;
}
