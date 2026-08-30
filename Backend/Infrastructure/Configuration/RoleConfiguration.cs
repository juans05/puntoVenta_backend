using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Configuration
{
    public class RoleConfiguration
    {
        public RoleConfiguration(EntityTypeBuilder<Role> entityBuilder)
        {
            entityBuilder.HasKey(x => x.Id);

            // Sin HasDefaultValue: con ese annotation, EF Core omite la columna del INSERT
            // cuando el valor en memoria coincide con el default de CLR para el tipo (0 para
            // int), y deja que la BD aplique su propio default (100) - lo que pisa en
            // silencio cualquier rol creado con Prioridad = 0 (la maxima prioridad posible).
            // El default de aplicacion ya vive en CreateRolePayload/UpdateRolePayload.
            entityBuilder.Property(x => x.Prioridad);

            entityBuilder.HasMany(e => e.UserRoles)
                         .WithOne(e => e.Role)
                         .HasForeignKey(e => e.RoleId)
                         .OnDelete(DeleteBehavior.Restrict)
                         .IsRequired();

            // El indice unico por defecto de ASP.NET Identity (RoleNameIndex) es solo sobre
            // NormalizedName: dos tenants distintos no podrian tener cada uno un rol "Ventas".
            // Se neutraliza y se reemplaza por uno compuesto con TenantId (TenantId NULL =
            // fila global, ej. SuperAdmin, que sigue siendo unica en todo el sistema).
            entityBuilder.HasIndex(x => x.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique(false);
            entityBuilder.HasIndex(x => new { x.NormalizedName, x.TenantId }).HasDatabaseName("RoleNameTenantIndex").IsUnique();
        }
    }
}
