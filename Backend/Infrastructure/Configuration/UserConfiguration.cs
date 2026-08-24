using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Identity;

namespace Infrastructure.Configuration
{
    public class UserConfiguration
    {
        public UserConfiguration(EntityTypeBuilder<User> entityBuilder)
        {
            entityBuilder.HasKey(x => x.Id);

            entityBuilder.HasMany(e => e.UserRoles)
                         .WithOne(e => e.User)
                         .HasForeignKey(e => e.UserId)
                         .OnDelete(DeleteBehavior.Restrict)
                         .IsRequired();

            entityBuilder.HasMany(e => e.UserSubmodules)
                         .WithOne(e => e.User)
                         .HasForeignKey(e => e.UserId)
                         .OnDelete(DeleteBehavior.Restrict)
                         .IsRequired();

            entityBuilder.HasOne(e => e.Sucursal)
                         .WithMany(s => s.Users)
                         .HasForeignKey(e => e.SucursalId)
                         .IsRequired(false)
                         .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
