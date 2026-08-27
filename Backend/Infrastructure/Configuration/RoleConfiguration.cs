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

            entityBuilder.Property(x => x.Prioridad).HasDefaultValue(100);

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
