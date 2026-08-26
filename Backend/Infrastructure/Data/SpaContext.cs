using Identitysoft.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Domain.Entities;
using Domain.Entities.Identity;
using Domain.Tenant;
using Infrastructure.Configuration;
using Application.Abstractions;

namespace Infrastructure.Data;

public class SpaContext : IdentityDbContext<User, Role, string>
{
    private readonly Tenantx _tenant;

    public string CurrentTenantName => _tenant?.Name;

    public int? CurrentSucursalId => _tenant?.SucursalId;

    public SpaContext(DbContextOptions<SpaContext> options, ITenantResolver tenantResolver) : base(options)
    {
        _tenant = tenantResolver.GetCurrentTenant();

        if (_tenant.ConnectionString is { } connectionString)

            Database.SetConnectionString(connectionString);
    }

    public DbSet<Cliente> Cliente => Set<Cliente>();
    public DbSet<ComprobanteCabecera> ComprobanteCabecera => Set<ComprobanteCabecera>();
    public DbSet<ComprobanteDetalle> ComprobanteDetalle => Set<ComprobanteDetalle>();
    public DbSet<Rubro> Rubro => Set<Rubro>();
    public DbSet<Pais> Pais => Set<Pais>();
    public DbSet<Moneda> Moneda => Set<Moneda>();
    public DbSet<Impuesto> Impuesto => Set<Impuesto>();
    public DbSet<Sucursal> Sucursal => Set<Sucursal>();
    public DbSet<RubroModulo> RubroModulo => Set<RubroModulo>();
    public DbSet<Metodopago> Metodopago => Set<Metodopago>();
    public DbSet<Pago> Pago => Set<Pago>();
    public DbSet<Producto> Producto => Set<Producto>();
    public DbSet<Proveedor> Proveedor => Set<Proveedor>();
    public DbSet<Comentario> Comentario => Set<Comentario>();
    public DbSet<Categoria> Categoria => Set<Categoria>();
    public DbSet<Grupo> Grupo => Set<Grupo>();
    public DbSet<Tenant> Tenant => Set<Tenant>();
    public DbSet<Empresa> Empresa => Set<Empresa>();
    public DbSet<EmpresaTenant> EmpresaTenant => Set<EmpresaTenant>();
    public DbSet<TipoDocumento> TipoDocumento => Set<TipoDocumento>();
    public DbSet<TipoDocumentoVenta> TipoDocumentoVenta => Set<TipoDocumentoVenta>();
    public DbSet<Seriecorrelativo> Seriecorrelativo => Set<Seriecorrelativo>();
    public DbSet<AspNetModule> AspNetModule => Set<AspNetModule>();
    public DbSet<AspNetSubModule> AspNetSubModule => Set<AspNetSubModule>();
    public DbSet<AspNetUserSubModule> AspNetUserSubModule => Set<AspNetUserSubModule>();
    public DbSet<Nacionalidad> Nacionalidad => Set<Nacionalidad>();
    public DbSet<Caja> Caja => Set<Caja>();
    public DbSet<CajaFisica> CajaFisica => Set<CajaFisica>();
    public DbSet<Ubigeo> Ubigeo => Set<Ubigeo>();
    public DbSet<Retiros> Retiros => Set<Retiros>();
    public DbSet<CorrelativoAnulacion> CorrelativoAnulacion => Set<CorrelativoAnulacion>();
    public DbSet<ConfiguracionFiscal> ConfiguracionFiscal => Set<ConfiguracionFiscal>();
    public DbSet<Recurso> Recurso => Set<Recurso>();
    public DbSet<ConfiguracionRenta> ConfiguracionRenta => Set<ConfiguracionRenta>();
    public DbSet<Anfitriona> Anfitriona => Set<Anfitriona>();
    public DbSet<Renta> Renta => Set<Renta>();
    public DbSet<RentaDetalle> RentaDetalle => Set<RentaDetalle>();
    public DbSet<InventoryMovement> InventoryMovement => Set<InventoryMovement>();
    public DbSet<AuditLog> AuditLog => Set<AuditLog>();
    public DbSet<Compra> Compra => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalle => Set<CompraDetalle>();
    public DbSet<Gasto> Gasto => Set<Gasto>();
    public DbSet<GastoPublicidad> GastoPublicidad => Set<GastoPublicidad>();
    public DbSet<CategoriaGasto> CategoriaGasto => Set<CategoriaGasto>();
    public DbSet<Ingreso> Ingreso => Set<Ingreso>();
    public DbSet<CierreDiario> CierreDiario => Set<CierreDiario>();
    public DbSet<WhatsappMessage> WhatsappMessage => Set<WhatsappMessage>();
    public DbSet<WhatsappConversation> WhatsappConversation => Set<WhatsappConversation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<ComprobanteCabecera>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<ComprobanteDetalle>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Metodopago>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<Pago>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Producto>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Categoria>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Grupo>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Proveedor>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<Comentario>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        // TipoDocumento y TipoDocumentoVenta son catálogos nacionales SUNAT (DNI/RUC/Pasaporte,
        // Boleta/Factura): idénticos para cualquier negocio, no hay filtro por tenant. Id es la
        // única PK (no compuesta con TenantId), así que dos tenants no pueden tener cada uno su
        // propia fila con el mismo Id — deben compartir las mismas filas.
        modelBuilder.Entity<Seriecorrelativo>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Sucursal>().HasQueryFilter(e => e.TenantId == _tenant.Name);

        //modelBuilder.Entity<AspNetModule>().HasQueryFilter(e => e.TenantId == _tenant.Name);

        //modelBuilder.Entity<AspNetSubModule>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<AspNetUserSubModule>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<Nacionalidad>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<Caja>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<CajaFisica>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Retiros>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<CorrelativoAnulacion>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Recurso>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<ConfiguracionRenta>(entity =>
        {
            entity.HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
            entity.Property(c => c.TurnosJson).HasColumnType("text");
            entity.Property(c => c.TarifasJson).HasColumnType("text");
            entity.Property(c => c.RecursosJson).HasColumnType("text");
        });
        modelBuilder.Entity<Anfitriona>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<Renta>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<RentaDetalle>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<InventoryMovement>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<Compra>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<CompraDetalle>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<Gasto>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<GastoPublicidad>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<CategoriaGasto>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<Ingreso>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<CierreDiario>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));
        modelBuilder.Entity<WhatsappMessage>().HasQueryFilter(e => e.TenantId == _tenant.Name);
        modelBuilder.Entity<WhatsappConversation>().HasQueryFilter(e => e.TenantId == _tenant.Name);

        new UserConfiguration(modelBuilder.Entity<User>());
        new RoleConfiguration(modelBuilder.Entity<Role>());
        new ComprobanteCabeceraConfiguration(modelBuilder.Entity<ComprobanteCabecera>());
        new UserSubmoduleConfiguration(modelBuilder.Entity<AspNetUserSubModule>());
        new SubmoduleConfiguration(modelBuilder.Entity<AspNetSubModule>());
        new ModuleConfiguration(modelBuilder.Entity<AspNetModule>());
        new UbigeoConfiguration(modelBuilder.Entity<Ubigeo>());
        new CajaConfiguration(modelBuilder.Entity<Caja>());
        new CajaFisicaConfiguration(modelBuilder.Entity<CajaFisica>());
        new PagoConfiguration(modelBuilder.Entity<Pago>());
        new TenantConfiguration(modelBuilder.Entity<Tenant>());
        new EmpresaTenantConfiguration(modelBuilder.Entity<EmpresaTenant>());
        new EmpresaConfiguration(modelBuilder.Entity<Empresa>());
        new ClienteConfiguration(modelBuilder.Entity<Cliente>());
        new ProductoConfiguration(modelBuilder.Entity<Producto>());
        new CorrelativoAnulacionConfiguration(modelBuilder.Entity<CorrelativoAnulacion>());
        new ConfiguracionFiscalConfiguration(modelBuilder.Entity<ConfiguracionFiscal>());
        new PaisConfiguration(modelBuilder.Entity<Pais>());
        new MonedaConfiguration(modelBuilder.Entity<Moneda>());
        new ImpuestoConfiguration(modelBuilder.Entity<Impuesto>());
        new SucursalConfiguration(modelBuilder.Entity<Sucursal>());
        new RubroModuloConfiguration(modelBuilder.Entity<RubroModulo>());
        new AuditLogConfiguration(modelBuilder.Entity<AuditLog>());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<string>().HaveMaxLength(150);
        builder.Properties<decimal>().HaveColumnType("decimal(13,2)");
        builder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
    }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    optionsBuilder
    //        .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
    //        .EnableSensitiveDataLogging(); // Esto mostrará los parámetros
    //}

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<EntityBase>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.TenantId = _tenant.Name;

            // Sede: solo se asigna si el tenant activo trae sucursal; filas sin sede (legacy) permanecen null
            if (_tenant.SucursalId.HasValue && entry.Entity is EntityBase)
            {
                var prop = entry.Entity.GetType().GetProperty("SucursalId");
                if (prop != null && prop.GetValue(entry.Entity) == null)
                    prop.SetValue(entry.Entity, _tenant.SucursalId.Value);
            }

            entry.Entity.FechaCreacion = DateTime.UtcNow.AddHours(-5);

            if (entry.Entity as EntityBase is AspNetUserToken || entry.Entity as EntityBase is CorrelativoAnulacion)
            { }
            else
            {
                entry.Entity.UsuarioCreacion = _tenant.Username;
            }
        }

        var auditSnapshot = ChangeTracker.Entries<EntityBase>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Where(e => e.Entity.GetType().Name != nameof(AuditLog))
            .Select(e => new { Entry = e, State = e.State, Valores = BuildAuditValores(e) })
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditSnapshot.Count > 0)
        {
            foreach (var item in auditSnapshot)
            {
                var entity = item.Entry.Entity;

                AuditLog.Add(new Domain.Entities.AuditLog
                {
                    TenantId = _tenant.Name,
                    UsuarioCreacion = _tenant.Username ?? entity.UsuarioCreacion,
                    FechaCreacion = DateTime.UtcNow.AddHours(-5),
                    Estado = true,
                    Accion = MapAuditAccion(item.State),
                    Entidad = entity.GetType().Name,
                    EntidadId = entity.Id == 0 ? null : entity.Id,
                    Valores = item.Valores
                });
            }

            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static string MapAuditAccion(EntityState state) => state switch
    {
        EntityState.Added => "CREAR",
        EntityState.Modified => "MODIFICAR",
        EntityState.Deleted => "ELIMINAR",
        _ => state.ToString()
    };

    private string? BuildAuditValores(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        try
        {
            var valores = new Dictionary<string, object?>();

            foreach (var prop in entry.CurrentValues.Properties)
            {
                if (prop.Name is "TenantId" or "UsuarioCreacion" or "SucursalId")
                    continue;

                valores[prop.Name] = entry.CurrentValues[prop];
            }

            return JsonConvert.SerializeObject(valores);
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> SaveChangesRegularAsync(CancellationToken cancellationToken = default)
    {  

        return await base.SaveChangesAsync(cancellationToken);
    }



    public void SeedUsersForTenant()
    {

        var newUser = new User
        {
            FirstName = "Admin",
            LastName = "Admin",
            FechaCreacion = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss"),
            UserName = "ADMIN",
            TenantId = 1,
            Estado = true,
            Email = "",
            EmailConfirmed = true,
        };

        var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(this), null, new PasswordHasher<IdentityUser>(), null, null, null, null, null, null);

        userManager.CreateAsync(newUser, "123456").Wait();
    }
}