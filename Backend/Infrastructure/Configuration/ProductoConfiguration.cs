using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identitysoft.Infrastructure.Configuration
{
    public class ProductoConfiguration
    {
        public ProductoConfiguration(EntityTypeBuilder<Producto> entityBuilder)
        {
            entityBuilder.Property(e => e.Precio).HasColumnType("decimal(13,2)");
            entityBuilder.Property(e => e.RutaImagen).HasColumnType("character varying (255)");
            entityBuilder.Property(e => e.CloudinaryPublicId).HasColumnType("character varying (255)");
        }
    }
}
