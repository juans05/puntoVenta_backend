using Domain.Payloads;

namespace Domain.DTO;

public class PreviewImportDto
{
    public int TotalFilas { get; set; }
    public int Validas { get; set; }
    public int ConError { get; set; }
    public List<ProductoCsvRow> Filas { get; set; } = new();
}