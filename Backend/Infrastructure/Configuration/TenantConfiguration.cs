using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration;

public class TenantConfiguration
{
    public TenantConfiguration(EntityTypeBuilder<Tenant> entityBuilder)
    {
        entityBuilder.HasKey(x => x.Identificador);

        entityBuilder.Property(x => x.Identificador)
            .ValueGeneratedNever();

        entityBuilder.Property(x => x.Activo)
            .HasDefaultValue(true);
    }
}
