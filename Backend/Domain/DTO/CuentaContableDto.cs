namespace Domain.DTO;

public class CuentaContableDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public int? CuentaPadreId { get; set; }
    public bool Estado { get; set; }
}
