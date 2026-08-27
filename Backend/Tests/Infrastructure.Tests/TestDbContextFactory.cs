using Application.Abstractions;
using AutoMapper;
using Domain.Common.Mappings;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

/// <summary>
/// Fake ITenantResolver: always returns the same tenant/sucursal, so every
/// query-filtered entity in the test DB is visible without extra plumbing.
/// </summary>
public class FakeTenantResolver : ITenantResolver
{
    private readonly Tenantx _tenant;

    public FakeTenantResolver(string tenantName = "TEST", int? sucursalId = null, string? username = "TEST_USER")
    {
        _tenant = new Tenantx { Name = tenantName, SucursalId = sucursalId, Username = username };
    }

    public Tenantx GetCurrentTenant() => _tenant;
}

public static class TestDbContextFactory
{
    private static readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MyAutomapper>()).CreateMapper();

    public static IMapper Mapper => _mapper;

    /// <summary>
    /// SQLite in-memory DB, one per test: the connection must stay open for the
    /// context's lifetime (closing it drops the DB). SQLite (not EF InMemory)
    /// because CrearComprobante/AnularVenta use real transactions, which the
    /// EF InMemory provider does not support.
    /// </summary>
    public static (SpaContext Context, SqliteConnection Connection) CreateContext(ITenantResolver? tenantResolver = null)
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SpaContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SpaContext(options, tenantResolver ?? new FakeTenantResolver());
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
