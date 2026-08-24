using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class EmpresaConfiguration
{
    public EmpresaConfiguration(EntityTypeBuilder<Empresa> entityBuilder)
    {
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
