using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Domain.DTO;

namespace WEB_API.Authorization;

public class SubmoduloAuthorizationHandler : AuthorizationHandler<SubmoduloRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SubmoduloRequirement requirement)
    {
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var rutasClaim = context.User.FindFirst("rutas")?.Value;
        if (!string.IsNullOrEmpty(rutasClaim))
        {
            try
            {
                var rutas = JsonConvert.DeserializeObject<List<AccesosDetalle>>(rutasClaim);
                var tieneAcceso = rutas?.Any(m => m.SubModulos?.Any(s => s.SubModulo == requirement.SubmoduloId) == true) == true;
                if (tieneAcceso)
                    context.Succeed(requirement);
            }
            catch (JsonException)
            {
                // claim malformado -> no se otorga acceso, cae al fallo por defecto de AuthorizationHandler
            }
        }

        return Task.CompletedTask;
    }
}
