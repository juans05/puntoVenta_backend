using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class CajaConfiguration
    {
        public CajaConfiguration(EntityTypeBuilder<Caja> entityBuilder)
        {
            //entityBuilder.Ignore(x => x.UsuarioCreacion);
            //entityBuilder.Ignore(x => x.FechaCreacion);
            //entityBuilder.Ignore(x => x.Estado);
            //entityBuilder.Ignore(x => x.TenantId);

        }
    }
}
