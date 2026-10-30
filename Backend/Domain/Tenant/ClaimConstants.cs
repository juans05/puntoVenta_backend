namespace Domain.Tenant;

public class ClaimConstants
{
    public const string TenantId = "https://schemas.microsoft.com/identity/claims/tenantid";
    public const string Sucursal = "sede";
    // Sede principal de la empresa: la que se usa cuando el usuario no tiene sede ni elige una.
    public const string SucursalPrincipal = "sedePrincipal";
    public const string Pais = "pais";
    public const string Rubro = "rubro";
    public const string Moneda = "moneda";
}