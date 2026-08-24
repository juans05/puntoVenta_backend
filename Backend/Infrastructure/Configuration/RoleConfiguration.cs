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

            entityBuilder.HasMany(e => e.UserRoles)
                         .WithOne(e => e.Role)
                         .HasForeignKey(e => e.RoleId)
                         .OnDelete(DeleteBehavior.Restrict)
                         .IsRequired();
        }
    }
}
