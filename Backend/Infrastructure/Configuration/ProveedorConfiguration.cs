using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ProveedorConfiguration
{
    public ProveedorConfiguration(EntityTypeBuilder<Proveedor> entityBuilder)
    {
        entityBuilder.Property(p => p.Codigo).HasMaxLength(20);
        entityBuilder.Property(p => p.DetalleAdicional).HasMaxLength(500);

        entityBuilder.HasOne(p => p.TipoDocumento)
                     .WithMany()
                     .HasForeignKey(p => p.TipoDocumentoId)
                     .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
    }
}
