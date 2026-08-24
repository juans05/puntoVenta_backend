using Application.Abstractions;
using Domain.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

public class MigrationDbContextFactory : IDesignTimeDbContextFactory<SpaContext>
{

    public SpaContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SpaContext>();

        builder.UseNpgsql();

        return new SpaContext(builder.Options, new MigrationTenantResolver());
    }

    private class MigrationTenantResolver : ITenantResolver
    {
        private readonly Tenantx _tenant;

        public MigrationTenantResolver()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production";

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddEnvironmentVariables()
                .Build();

            var defaultConnection = configuration.GetSection("TenantOptions").GetValue<string>("DefaultConnection")
                ?? configuration.GetValue<string>("DefaultConnection");

            _tenant = new Tenantx { Name = "public", ConnectionString = defaultConnection };

            if (string.IsNullOrEmpty(defaultConnection))
            {
                throw new InvalidOperationException(
                    "No se encontró la cadena de conexión por defecto (TenantOptions:DefaultConnection o DefaultConnection) en appsettings.json.");
            }
        }

        public Tenantx GetCurrentTenant() => _tenant;
    }
}