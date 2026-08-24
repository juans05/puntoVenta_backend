using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ComprobanteCabeceraConfiguration
{
    public ComprobanteCabeceraConfiguration(EntityTypeBuilder<ComprobanteCabecera> entityBuilder)
    {
        entityBuilder.Property(u => u.EstadoComprobante)
                      .IsRequired()
                      .HasMaxLength(1)
                      .HasDefaultValue('C');

        entityBuilder.Property(u => u.EnviadoSunat)
                      .IsRequired()
                      .HasMaxLength(1)
                      .HasDefaultValue('P');

        entityBuilder.Property(u => u.Serie)
                     .IsRequired()
                     .HasMaxLength(4);

        entityBuilder.Property(u => u.NumeroDocumento)
                     .HasMaxLength(15);


        entityBuilder.Property(u => u.RazonSocial)
                     .HasMaxLength(100);

        entityBuilder.Property(u => u.TotalLetras)
                     .HasMaxLength(120);

        entityBuilder.Property(u => u.MensajeSunat)
                     .HasMaxLength(250);

        entityBuilder.Property(u => u.UsuarioCreacion)
                     .HasMaxLength(15);

        entityBuilder.Property(u => u.TenantId)
                     .HasMaxLength(15);

        entityBuilder.Property(u => u.TicketSunat)
                     .HasMaxLength(20);

        entityBuilder.Property(u => u.EnvioAnulacionSunat)
                      .HasDefaultValue(false);

        entityBuilder.Property(u => u.TipoEnvio)
                     .HasMaxLength(20);

        entityBuilder.Property(u => u.Distrito)
                     .HasMaxLength(100);

        entityBuilder.Ignore(u => u.Estado);
    }
}
