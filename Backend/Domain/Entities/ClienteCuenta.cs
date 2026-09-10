namespace Domain.Entities;

public class ClienteCuenta : EntityBase
{
    public int ClienteId { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public Cliente Cliente { get; set; } = null!;
}