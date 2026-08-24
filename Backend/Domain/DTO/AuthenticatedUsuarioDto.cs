namespace Domain.DTO;


public class AuthenticatedUsuarioDto
{
    public string UserName { get; private set; }
    public List<string> Roles { get; private set; }
    public List<AccesosDetalle> Rutas { get; private set; }


    public AuthenticatedUsuarioDto(string username, List<string> roles, List<AccesosDetalle> rutas)
    {
        UserName = username;
        Roles = roles;
        Rutas = rutas;
    }
}