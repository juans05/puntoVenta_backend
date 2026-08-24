using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class UbigeoConfiguration
{
    public UbigeoConfiguration(EntityTypeBuilder<Ubigeo> entityBuilder)
    {
        entityBuilder.HasKey(e => e.UbigeoId);
        entityBuilder.Property(e => e.UbigeoId).HasMaxLength(7).IsRequired();
        entityBuilder.Property(e => e.Departamento).HasMaxLength(30);
        entityBuilder.Property(e => e.Provincia).HasMaxLength(60);
        entityBuilder.Property(e => e.Distrito).HasMaxLength(60);
    }
}