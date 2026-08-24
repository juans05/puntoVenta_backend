using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class SucursalConfiguration
{
    public SucursalConfiguration(EntityTypeBuilder<Sucursal> entityBuilder)
    {
        entityBuilder.Property(s => s.Nombre).IsRequired().HasMaxLength(200);
        entityBuilder.Property(s => s.Direccion).HasMaxLength(300);
        entityBuilder.Property(s => s.UbigeoId).HasMaxLength(10);
        entityBuilder.Property(s => s.Latitud).HasColumnType("decimal(10,7)");
        entityBuilder.Property(s => s.Longitud).HasColumnType("decimal(10,7)");
        entityBuilder.Property(s => s.TenantId).HasMaxLength(15);
        entityBuilder.Property(s => s.UsuarioCreacion).HasMaxLength(15);

        entityBuilder.HasOne(s => s.Pais)
                      .WithMany()
                      .HasForeignKey(s => s.PaisId)
                      .OnDelete(DeleteBehavior.Restrict);

        entityBuilder.HasOne(s => s.Moneda)
                      .WithMany(m => m.Sucursales)
                      .HasForeignKey(s => s.MonedaId)
                      .OnDelete(DeleteBehavior.Restrict);

        entityBuilder.HasOne(s => s.Rubro)
                      .WithMany(r => r.Sucursales)
                      .HasForeignKey(s => s.RubroId)
                      .OnDelete(DeleteBehavior.Restrict);
    }
}