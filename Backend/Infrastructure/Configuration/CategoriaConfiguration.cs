using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class CategoriaConfiguration
    {
        public CategoriaConfiguration(EntityTypeBuilder<Categoria> entityBuilder)
        {
            //entityBuilder.HasKey(x => x.Id);

            //entityBuilder.HasMany(e => e.Productos)
            //             .WithOne(e => e.Categoria)
            //             .HasForeignKey(e => e.CategoriaId)
            //             .IsRequired();

            //NO ES NECESARIO AGREGAR ESTO YA QUE SE HACE DE MANERA AUTOMATICA
            //SI LO DESCOMENTAMOS FUNCIONA DE IGUAL FORMA
        }
    }
}
