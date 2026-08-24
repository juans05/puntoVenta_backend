using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class PaisConfiguration
{
    public PaisConfiguration(EntityTypeBuilder<Pais> entityBuilder)
    {
        entityBuilder.Property(p => p.Codigo).IsRequired().HasMaxLength(2);
        entityBuilder.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
        entityBuilder.Property(p => p.Idioma).IsRequired().HasMaxLength(50);
        entityBuilder.Property(p => p.MonedaCodigo).IsRequired().HasMaxLength(3);
        entityBuilder.Property(p => p.TimeZone).IsRequired().HasMaxLength(100);
        entityBuilder.Property(p => p.EsquemaFiscal).IsRequired().HasMaxLength(50);

        entityBuilder.HasIndex(p => p.Codigo).IsUnique();
    }
}