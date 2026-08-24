using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class ImpuestoConfiguration
{
    public ImpuestoConfiguration(EntityTypeBuilder<Impuesto> entityBuilder)
    {
        entityBuilder.Property(i => i.Nombre).IsRequired().HasMaxLength(100);
        entityBuilder.Property(i => i.Porcentaje).HasColumnType("decimal(5,2)");
        entityBuilder.Property(i => i.AplicableA).HasMaxLength(50);

        entityBuilder.HasOne(i => i.Pais)
                      .WithMany()
                      .HasForeignKey(i => i.PaisId)
                      .OnDelete(DeleteBehavior.Restrict);
    }
}