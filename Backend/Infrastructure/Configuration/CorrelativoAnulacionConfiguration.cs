using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identitysoft.Infrastructure.Configuration;

public class CorrelativoAnulacionConfiguration
{
    public CorrelativoAnulacionConfiguration(EntityTypeBuilder<CorrelativoAnulacion> entityBuilder)
    {
        entityBuilder.Ignore(e => e.Estado);
        entityBuilder.Ignore(e => e.UsuarioCreacion);
    }
}
