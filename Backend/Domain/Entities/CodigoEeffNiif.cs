namespace Domain.Entities;

// Catalogo fijo de codificacion NIIF para el Estado de Situacion Financiera y el Estado de
// Resultados (ver "configuracion plan de cuentas ERPdocx.docx"). Es un estandar, no se
// personaliza por tenant -- catalogo global, sembrado una vez via migracion.
public class CodigoEeffNiif : EntityBase
{
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Categoria { get; set; } = null!;
}
