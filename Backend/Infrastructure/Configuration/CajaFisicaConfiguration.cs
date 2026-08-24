using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class CajaFisicaConfiguration
{
    public CajaFisicaConfiguration(EntityTypeBuilder<CajaFisica> entityBuilder)
    {
        entityBuilder.Property(e => e.Nombre).HasMaxLength(60).IsRequired();
        entityBuilder.HasOne(e => e.Sucursal)
                     .WithMany()
                     .HasForeignKey(e => e.SucursalId);
    }
}