using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Identity;

namespace Infrastructure.Configuration;

public class ModuleConfiguration
{
    public ModuleConfiguration(EntityTypeBuilder<AspNetModule> entityBuilder)
    {
        entityBuilder.HasKey(x => x.Identificador);
        entityBuilder.Ignore(e => e.Id);
        //se ignora el tenant id para reutilizar los modulos
        entityBuilder.Ignore(e => e.TenantId);

    }
}
