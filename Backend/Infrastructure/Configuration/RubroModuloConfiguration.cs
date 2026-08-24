using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class RubroModuloConfiguration
{
    public RubroModuloConfiguration(EntityTypeBuilder<RubroModulo> entityBuilder)
    {
        entityBuilder.Property(r => r.CodigoModulo).IsRequired().HasMaxLength(50);

        entityBuilder.HasIndex(r => new { r.RubroId, r.CodigoModulo }).IsUnique();

        entityBuilder.HasOne(r => r.Rubro)
                      .WithMany(r => r.Moduulos)
                      .HasForeignKey(r => r.RubroId)
                      .OnDelete(DeleteBehavior.Cascade);
    }
}