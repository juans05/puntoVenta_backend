using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class SalonConfiguration
{
    public SalonConfiguration(EntityTypeBuilder<Salon> entityBuilder)
    {
        entityBuilder.HasKey(e => e.Id);
        entityBuilder.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
        entityBuilder.Property(e => e.UbigeoId).IsRequired().HasMaxLength(10);
        entityBuilder.Property(e => e.Activo).HasDefaultValue(true);

        entityBuilder.HasOne(e => e.Ubigeo)
                     .WithMany()
                     .HasForeignKey(e => e.UbigeoId)
                     .IsRequired()
                     .OnDelete(DeleteBehavior.Restrict);
    }
}
