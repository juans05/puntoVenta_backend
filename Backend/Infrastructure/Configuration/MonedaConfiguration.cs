using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class MonedaConfiguration
{
    public MonedaConfiguration(EntityTypeBuilder<Moneda> entityBuilder)
    {
        entityBuilder.Property(m => m.Codigo).IsRequired().HasMaxLength(3);
        entityBuilder.Property(m => m.Simbolo).IsRequired().HasMaxLength(5);
        entityBuilder.Property(m => m.Locale).IsRequired().HasMaxLength(20);

        entityBuilder.HasIndex(m => m.Codigo).IsUnique();

        entityBuilder.HasOne(m => m.Pais)
                      .WithMany(p => p.Monedas)
                      .HasForeignKey(m => m.PaisId)
                      .OnDelete(DeleteBehavior.Restrict);
    }
}