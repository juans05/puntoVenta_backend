using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ClienteConfiguration
{
    public ClienteConfiguration(EntityTypeBuilder<Cliente> entityBuilder)
    {

        entityBuilder.Property(e => e.Sexo)
                     .HasColumnType("character varying (1)");

        entityBuilder.HasOne(e => e.Ubigeo)
                     .WithMany()
                     .HasForeignKey(e => e.UbigeoId)
                     .IsRequired(false)
                     .OnDelete(DeleteBehavior.Restrict);

        entityBuilder.Property(e => e.UbigeoId)
                      .HasMaxLength(10)
                      .IsRequired(false);

    }
}
