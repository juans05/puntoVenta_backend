using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class UserSubmoduleConfiguration
    {
        public UserSubmoduleConfiguration(EntityTypeBuilder<AspNetUserSubModule> entityBuilder)
        {

            // TenantId en la clave: un usuario con acceso a varias empresas tiene permisos propios en cada una.
            entityBuilder.HasKey(e => new { e.UserId, e.SubmoduleId, e.TenantId });

            entityBuilder.HasOne(d => d.User)
                           .WithMany(p => p.UserSubmodules)
                           .HasForeignKey(d => d.UserId)
                           .OnDelete(DeleteBehavior.Restrict);

            entityBuilder.HasOne(d => d.Submodule)
                            .WithMany(p => p.UserSubmodules)
                            .HasForeignKey(d => d.SubmoduleId)
                            .OnDelete(DeleteBehavior.Restrict);

            entityBuilder.Ignore(e => e.Id);

        }
    }
}
