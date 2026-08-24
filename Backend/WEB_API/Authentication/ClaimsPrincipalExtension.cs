using Newtonsoft.Json;
using Domain.DTO;
using Domain.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WEB_API.Authentication
{
    public static class ClaimsPrincipalExtension
    {
        public static AuthenticatedUser GetUser(this ClaimsPrincipal principal)
        {
            var userName = principal.FindFirstValue("username");

            var tenant = principal.FindFirstValue(ClaimConstants.TenantId);

            var nombre = principal.FindFirstValue("nombre");

            var profiles = principal.FindFirstValue("rutas");

            var empresa = principal.FindFirstValue("empresa");

            var accesosDetalles = JsonConvert.DeserializeObject<List<AccesosDetalle>>(profiles);

            var isSuperAdmin = principal.IsInRole("SuperAdmin");

            return new AuthenticatedUser(userName, tenant, accesosDetalles, nombre, empresa, isSuperAdmin);
        }
    }


    public sealed class AuthenticatedUser
    {
        public string UserName { get; private set; }

        public string Tenant { get; private set; }

        public string Nombre { get; private set; }

        public string Empresa { get; private set; }

        public List<AccesosDetalle> Rutas { get; private set; }

        public bool IsSuperAdmin { get; private set; }

        public AuthenticatedUser(string username, string tenant, List<AccesosDetalle> rutas, string nombre, string empresa, bool isSuperAdmin)
        {
            UserName = username;
            Tenant = tenant;
            Rutas = rutas;
            Nombre = nombre;
            Empresa = empresa;
            IsSuperAdmin = isSuperAdmin;
        }
    }
}