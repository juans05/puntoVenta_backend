namespace Domain.DTO;

public class DepartamentoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public List<AprobadorDto> Aprobadores { get; set; } = new();
}

public class AprobadorDto
{
    public string UserId { get; set; } = null!;
    public string? Nombre { get; set; }
}
