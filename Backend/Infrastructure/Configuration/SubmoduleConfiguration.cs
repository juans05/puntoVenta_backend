using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Identity;

namespace Infrastructure.Configuration
{
    public class SubmoduleConfiguration
    {
        public SubmoduleConfiguration(EntityTypeBuilder<AspNetSubModule> entityBuilder)
        {
            entityBuilder.HasKey(x => x.Identificador);

            entityBuilder.HasOne(d => d.Module)
                         .WithMany(p => p.Submodules)
                         .HasForeignKey(d => d.ModuloId);

            entityBuilder.Ignore(e => e.Id);
            entityBuilder.Ignore(e => e.TenantId);

        }
    }
}
