using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class PagoConfiguration
{
    public PagoConfiguration(EntityTypeBuilder<Pago> entityBuilder)
    {
        //entityBuilder.Property(x => x.IdentificadorCaja).HasMaxLength(50);
    }
}
