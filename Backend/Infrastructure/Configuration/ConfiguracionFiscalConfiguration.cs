using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class ConfiguracionFiscalConfiguration
{
    public ConfiguracionFiscalConfiguration(EntityTypeBuilder<ConfiguracionFiscal> entityBuilder)
    {
        entityBuilder.HasOne(e => e.Empresa)
                      .WithMany()
                      .HasForeignKey(e => e.EmpresaId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

        entityBuilder.Property(e => e.Pais)
                      .HasMaxLength(2);

        entityBuilder.Property(e => e.Ruc)
                      .HasMaxLength(20);

        entityBuilder.Property(e => e.RazonSocial)
                      .HasMaxLength(200);

        entityBuilder.Property(e => e.NombreComercial)
                      .HasMaxLength(200);

        entityBuilder.Property(e => e.Direccion)
                      .HasMaxLength(300);

        entityBuilder.Property(e => e.UbigeoId)
                      .HasMaxLength(10)
                      .IsRequired(false);

        entityBuilder.Property(e => e.Departamento)
                      .HasMaxLength(100);

        entityBuilder.Property(e => e.Provincia)
                      .HasMaxLength(100);

        entityBuilder.Property(e => e.Distrito)
                      .HasMaxLength(100);

        entityBuilder.Property(e => e.SerieFactura)
                      .HasMaxLength(4);

        entityBuilder.Property(e => e.SerieBoleta)
                      .HasMaxLength(4);

        entityBuilder.Property(e => e.SerieNota)
                      .HasMaxLength(4);

        entityBuilder.Property(e => e.CodigoAdaptador)
                      .HasMaxLength(50);

        entityBuilder.Property(e => e.Moneda)
                      .HasMaxLength(3);

        entityBuilder.Property(e => e.TenantId)
                      .HasMaxLength(15);

        entityBuilder.Property(e => e.UsuarioCreacion)
                      .HasMaxLength(15);
    }
}