using Microsoft.AspNetCore.Authorization;

namespace WEB_API.Authorization;

// Piloto de enforcement real de permisos: hasta ahora el sistema Role->RoleSubmodule solo
// controlaba que veia el Sidebar en el frontend, ningun endpoint del backend lo validaba. Este
// requirement exige que el claim "rutas" del JWT (ver AuthenticationRepository/
// ClaimsPrincipalExtension) incluya el SubModulo indicado, o que el usuario sea SuperAdmin.
public class SubmoduloRequirement : IAuthorizationRequirement
{
    public string SubmoduloId { get; }

    public SubmoduloRequirement(string submoduloId)
    {
        SubmoduloId = submoduloId;
    }
}
