using Application.Abstractions;
using AutoMapper;
using Domain.Common.Mappings;
using Domain.Entities;
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
        SeedCuentasContables(context);

        return (context, connection);
    }

    // Subconjunto minimo del plan de cuentas (PCGE) que usan los asientos automaticos de
    // OrdenCompraRepository/CompraRepository/CuentasRepository (Movimiento/Factura/Pago) -- ver
    // Infrastructure/Data/Default/cuentacontable.json para el catalogo real completo, que aqui
    // seria demasiado para cada test. Inocuo para los tests que no tocan estos flujos.
    private static void SeedCuentasContables(SpaContext context)
    {
        var tenantId = context.CurrentTenantName;
        context.CuentaContable.AddRange(
            new CuentaContable { Codigo = "10", Nombre = "Efectivo y Equivalentes de Efectivo", Tipo = TipoCuentaContable.Activo, TenantId = tenantId },
            new CuentaContable { Codigo = "20", Nombre = "Mercaderías", Tipo = TipoCuentaContable.Activo, TenantId = tenantId },
            new CuentaContable { Codigo = "42", Nombre = "Cuentas por Pagar Comerciales Terceros", Tipo = TipoCuentaContable.Pasivo, TenantId = tenantId },
            new CuentaContable { Codigo = "4211", Nombre = "Facturas, boletas y otros comp. No emitidas", Tipo = TipoCuentaContable.Pasivo, TenantId = tenantId },
            new CuentaContable { Codigo = "63", Nombre = "Gastos de Servicios Prestados por Terceros", Tipo = TipoCuentaContable.Gasto, TenantId = tenantId },
            new CuentaContable { Codigo = "12", Nombre = "Cuentas por Cobrar Comerciales-Terceros", Tipo = TipoCuentaContable.Activo, TenantId = tenantId },
            new CuentaContable { Codigo = "70", Nombre = "Ventas", Tipo = TipoCuentaContable.Ingreso, TenantId = tenantId },
            new CuentaContable { Codigo = "69", Nombre = "Costos de Ventas", Tipo = TipoCuentaContable.Gasto, TenantId = tenantId });
        context.SaveChanges();
    }

    /// <summary>
    /// EF Core InMemory provider: for read-only repositories that never call
    /// BeginTransactionAsync. Needed because the SQLite provider can't translate
    /// a decimal SUM over an arithmetic expression (Cantidad * CostoUnitario) -
    /// a provider quirk, not a bug in the query (Npgsql handles it fine).
    /// </summary>
    public static SpaContext CreateInMemoryContext(ITenantResolver? tenantResolver = null)
    {
        var options = new DbContextOptionsBuilder<SpaContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SpaContext(options, tenantResolver ?? new FakeTenantResolver());
    }
}
