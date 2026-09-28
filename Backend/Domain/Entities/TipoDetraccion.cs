namespace Domain.Entities;

// Catalogo de porcentajes de detraccion SUNAT (R. 183-2004/SUNAT y anexos). TenantId null =
// catalogo base compartido (los 5 valores originales: 0/4/10/12/15%), no-null = variante agregada
// por ese tenant via CRUD -- mismo criterio que Moneda/TipoIgv (ver comentarios en esas entidades).
public class TipoDetraccion : EntityBase
{
    public decimal Porcentaje { get; set; }
    public string? Descripcion { get; set; }
}
