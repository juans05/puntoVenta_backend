using Application.Abstractions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WEB_API;

public static class DatabaseHelper
{
    public static void EnsureLatestDatabase(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();

        var connections = provider.GetRequiredService<ITenantRegistry>()
                                  .GetTenants()
                                  .Select(e => e.ConnectionString)
                                  .Distinct();

        foreach (var connection in connections)
        {
        }
            var db = new MigrationDbContextFactory().CreateDbContext(Array.Empty<string>());
            //db.Database.SetConnectionString("Server=prod.c3zi9vhwcuwh.us-east-1.rds.amazonaws.com;Port=5432;Database=Spa;User Id=postgres;Password=developers2023.;");
            db.Database.SetConnectionString("Server=localhost;Port=5432;Database=Spa;User Id=postgres;Password=123;");

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            //if (db.Database.GetPendingMigrations().Any()) db.Database.Migrate();

            db.SeedUsersForTenant();//esto una sola vez al crear el comercio
    }
}