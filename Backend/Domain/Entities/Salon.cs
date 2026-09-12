namespace Domain.Entities;

public class Salon
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string UbigeoId { get; set; } = null!;
    public bool Activo { get; set; } = true;

    public Ubigeo? Ubigeo { get; set; }
}
