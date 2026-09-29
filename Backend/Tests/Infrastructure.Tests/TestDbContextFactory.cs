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
        SeedSucursalBase(context);

        return (context, connection);
    }

    // StockSucursalHelper.Ajustar cae a "la primera sucursal del tenant" cuando ni el
    // documento ni el producto traen SucursalId -- en produccion siempre hay al menos una
    // (todo tenant real se siembra con una). Sin esto, cualquier test que mueva stock sin
    // fijar SucursalId explicito fallaria con "No hay una sucursal registrada...".
    private static void SeedSucursalBase(SpaContext context)
    {
        // TenantId puesto a mano (no via SaveChangesAsync, que es donde SpaContext lo
        // autoasigna) -- mismo motivo que SeedCuentasContables: aqui se usa el SaveChanges
        // sincrono, que no pasa por ese override. Pais.Codigo es unico global (no por tenant),
        // asi que si algun test crea su propio Pais "PE" (ComprobanteRepositoryTests.SeedSucursalAsync)
        // hay que reusarlo en vez de duplicarlo.
        var tenantId = context.CurrentTenantName;
        var pais = context.Pais.FirstOrDefault(p => p.Codigo == "PE");
        if (pais == null)
        {
            pais = new Pais { Codigo = "PE", Nombre = "Peru", Idioma = "es", MonedaCodigo = "PEN", TimeZone = "America/Lima", EsquemaFiscal = "SUNAT" };
            context.Pais.Add(pais);
            context.SaveChanges();
        }

        var moneda = new Moneda { Codigo = "PEN", Simbolo = "S/", Locale = "es-PE", PaisId = pais.Id, TenantId = tenantId };
        context.Moneda.Add(moneda);

        var rubro = new Rubro { Nombre = "General" };
        context.Rubro.Add(rubro);
        context.SaveChanges();

        context.Sucursal.Add(new Sucursal { Nombre = "Sucursal Test", MonedaId = moneda.Id, PaisId = pais.Id, RubroId = rubro.Id, TenantId = tenantId });
        context.SaveChanges();
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
            new CuentaContable { Codigo = "69", Nombre = "Costos de Ventas", Tipo = TipoCuentaContable.Gasto, TenantId = tenantId },
            new CuentaContable { Codigo = "40111", Nombre = "IGV – Cuenta propia", Tipo = TipoCuentaContable.Pasivo, TenantId = tenantId });
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
