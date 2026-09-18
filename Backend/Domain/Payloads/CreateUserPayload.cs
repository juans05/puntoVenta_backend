using System.ComponentModel.DataAnnotations;

namespace Domain.Payloads
{
    public class CreateUserPayload
    {
        [Required]
        public string FirstName { get; set; }

        public string? LastName { get; set; }

        [Required]
        public string UserName { get; set; }

        public string? Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string? Phone { get; set; }

        public string? Celular { get; set; }

        public string? Avatar { get; set; }

        public int? SucursalId { get; set; }

        // Si true, se le da acceso completo (todos los submodulos) de inmediato -- mismo criterio
        // que ya usa el sistema para el admin fundador de un tenant. Si false y RoleId viene, se le
        // asigna ese rol (creado en Roles y Permisos); si no viene ninguno, queda sin acceso hasta
        // que alguien se lo asigne despues.
        public bool EsAdministrador { get; set; }

        public string? RoleId { get; set; }

    }
}
