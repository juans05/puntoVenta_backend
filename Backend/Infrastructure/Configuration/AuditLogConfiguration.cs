using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identitysoft.Infrastructure.Configuration
{
    public class AuditLogConfiguration
    {
        public AuditLogConfiguration(EntityTypeBuilder<AuditLog> entityBuilder)
        {
            entityBuilder.Property(e => e.Valores).HasColumnType("text");
        }
    }
}