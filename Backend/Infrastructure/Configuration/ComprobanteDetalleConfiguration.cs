using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class ComprobanteDetalleConfiguration
    {
        public ComprobanteDetalleConfiguration(EntityTypeBuilder<ComprobanteDetalle> entityBuilder)
        {
            entityBuilder.Property(e => e.ValorUnitario).HasColumnType("decimal(13,2)");
        }
    }
}
