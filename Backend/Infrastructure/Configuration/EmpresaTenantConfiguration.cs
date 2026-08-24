using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configuration
{
    public class EmpresaTenantConfiguration
    {
        public EmpresaTenantConfiguration(EntityTypeBuilder<EmpresaTenant> entityBuilder)
        {
            entityBuilder.HasKey(e => new { e.EmpresaId, e.TenantId });

            entityBuilder.HasOne(d => d.Empresa)
                          .WithMany(p => p.EmpresaTenants)
                          .HasForeignKey(d => d.EmpresaId)
                          .OnDelete(DeleteBehavior.Restrict);

            entityBuilder.HasOne(d => d.Tenant)
                          .WithMany(p => p.EmpresaTenants)
                          .HasForeignKey(d => d.TenantId)
                          .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
