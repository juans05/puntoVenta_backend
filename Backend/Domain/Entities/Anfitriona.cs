namespace Domain.Entities;

public class Anfitriona : EntityBase
{
    public string Nombres { get; set; } = null!;

    public string? Apellidos { get; set; }

    public int? NacionalidadId { get; set; }

    public Nacionalidad? Nacionalidad { get; set; }

    public string? Direccion { get; set; }

    public string? Celular { get; set; }

    public string? Foto { get; set; }
}