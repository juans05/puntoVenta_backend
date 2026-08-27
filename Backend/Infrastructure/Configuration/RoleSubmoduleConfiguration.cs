using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class RoleSubmoduleConfiguration
    {
        public RoleSubmoduleConfiguration(EntityTypeBuilder<RoleSubmodule> entityBuilder)
        {
            entityBuilder.HasKey(e => new { e.RoleId, e.SubmoduleId });

            entityBuilder.HasOne(d => d.Role)
                         .WithMany(p => p.RoleSubmodules)
                         .HasForeignKey(d => d.RoleId)
                         .OnDelete(DeleteBehavior.Cascade);

            entityBuilder.HasOne(d => d.Submodule)
                         .WithMany()
                         .HasForeignKey(d => d.SubmoduleId)
                         .OnDelete(DeleteBehavior.Restrict);

            entityBuilder.Ignore(e => e.Id);
        }
    }
}
