using System.ComponentModel.DataAnnotations;

namespace Domain.Payloads;

public static class OpcionPlanCuentas
{
    public const string Defecto = "DEFECTO"; // PCGE por defecto del sistema
    public const string Blanco = "BLANCO";   // sin cuentas: el usuario arma su plan
    public const string Copiar = "COPIAR";   // copia el plan de otra empresa a la que tiene acceso
}

// Empresa nueva creada por el dueño de la cuenta desde "Mis empresas".
public class CrearEmpresaPropietarioPayload
{
    [Required]
    public string RazonSocial { get; set; } = null!;
    public string? NombreComercial { get; set; }
    public string? Ruc { get; set; }
    public string? Direccion { get; set; }
    // Sin rubro se usa el de la empresa activa.
    public int? RubroId { get; set; }
    public string PlanCuentas { get; set; } = OpcionPlanCuentas.Defecto;
    public int? CopiarDeTenantId { get; set; }
}
