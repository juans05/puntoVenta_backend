# Roles y permisos por tenant — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar los checks de rol hardcodeados por username (VENTAS1/VENTAS2/RECEPCION/CONTADORA/JRAMIREZ/DISCOBAR/SPASOLIS, y los prefijos genéricos) por un sistema de roles real, gestionable por el admin de cada tenant, que decide tanto qué pantallas ve un usuario como a dónde aterriza tras el login.

**Architecture:** `Role` (ASP.NET Identity, ya existe) gana `TenantId`/`RutaPorDefecto`/`Prioridad`. Tabla nueva `RoleSubmodule` reemplaza la asignación directa Usuario↔Submódulo (`AspNetUserSubModule`, que deja de recibir escrituras nuevas). El JWT sigue llevando `rutas` (mismo shape que hoy) y suma un claim `rutaPorDefecto`, ambos calculados como la unión/prioridad de los roles del usuario. Un admin CRUD de roles + asignación de roles a usuarios en el frontend.

**Tech Stack:** Backend: .NET 6, EF Core 6, Npgsql, ASP.NET Identity (`UserManager<User>`, `RoleManager<Role>`). Frontend: React + Redux, react-router-dom, axios. Tests: xUnit contra SQLite en memoria (`Backend/Tests/Infrastructure.Tests`, ya existe).

**Spec:** `Backend/docs/superpowers/specs/2026-08-27-roles-permisos-design.md`

## Global Constraints

- Cada `Role` pertenece a un único tenant (`TenantId`), salvo el rol global `SuperAdmin` (`TenantId = null`) que ya existe y no se toca.
- Un usuario puede tener varios roles; sus permisos de pantalla son la unión de los submódulos de todos sus roles; la ruta de aterrizaje es la del rol con `Prioridad` más baja.
- No generalizar la restricción de categoría/grupo de VENTAS1/VENTAS2 — se migra a `roles.includes('Ventas')` únicamente, sin nuevo campo de datos.
- No tocar `AspNetUserSubModule` (se deja de escribir, no se borra en este plan).
- Todo query nuevo sobre entidades con `TenantId` sigue el patrón de query filter ya usado en `SpaContext.OnModelCreating`.
- Todo repositorio/servicio/controller nuevo sigue el patrón exacto ya usado en el resto del código: tuplas `(ServiceStatus, T, string)` en repositorios, `MessageResult<T>` + `ErrorHandler` en servicios, rutas `api/<recurso>` en controllers, registro en `Infrastructure/DependencyInjection.cs`.

---

## Task 1: Borrar las entidades muertas Module/Submodule/UserSubmodule

Estas tres clases (sin prefijo `AspNet`) no tienen `DbSet` en `SpaContext` y no las usa ningún archivo real (verificado con grep). No confundir con `AspNetModule`/`AspNetSubModule`/`AspNetUserSubModule`, que sí se usan y no se tocan en este task.

**Files:**
- Delete: `Backend/Domain/Entities/Identity/Module.cs`
- Delete: `Backend/Domain/Entities/Identity/Submodule.cs`
- Delete: `Backend/Domain/Entities/Identity/UserSubmodule.cs`

**Interfaces:**
- Consumes: nada.
- Produces: nada (esto es limpieza pura).

- [ ] **Step 1: Confirmar que no hay referencias antes de borrar**

Run: `cd Backend && grep -rn "Identity.Module\b\|Identity.Submodule\b\|Identity.UserSubmodule\b" --include="*.cs" . | grep -v bin | grep -v obj`

Expected: solo aparecen los propios archivos `Module.cs`, `Submodule.cs`, `UserSubmodule.cs` (nadie más los importa). Si aparece algo más, PARAR y no borrar ese archivo — investigar el uso primero.

- [ ] **Step 2: Borrar los tres archivos**

```bash
cd Backend
rm Domain/Entities/Identity/Module.cs
rm Domain/Entities/Identity/Submodule.cs
rm Domain/Entities/Identity/UserSubmodule.cs
```

- [ ] **Step 3: Compilar para confirmar que no rompió nada**

Run: `cd Backend && dotnet build --nologo -v quiet 2>&1 | grep -i error`
Expected: `0 Errores` (puede haber warnings preexistentes, ignorarlos).

- [ ] **Step 4: Commit**

```bash
cd Backend
git add Domain/Entities/Identity/Module.cs Domain/Entities/Identity/Submodule.cs Domain/Entities/Identity/UserSubmodule.cs
git commit -m "chore: borrar entidades Module/Submodule/UserSubmodule sin uso

Duplicaban AspNetModule/AspNetSubModule/AspNetUserSubModule (que si se
usan). Sin DbSet registrado, sin ninguna referencia real en el codigo."
```

---

## Task 2: Modelo de datos — Role gana TenantId/RutaPorDefecto/Prioridad, tabla RoleSubmodule

**Files:**
- Modify: `Backend/Domain/Entities/Identity/Role.cs`
- Create: `Backend/Domain/Entities/Identity/RoleSubmodule.cs`
- Modify: `Backend/Infrastructure/Configuration/RoleConfiguration.cs`
- Create: `Backend/Infrastructure/Configuration/RoleSubmoduleConfiguration.cs`
- Modify: `Backend/Infrastructure/Data/SpaContext.cs`
- Create (via `dotnet ef migrations add`): `Backend/Infrastructure/Migrations/<timestamp>_AddRoleTenantAndRoleSubmodule.cs` (+ `.Designer.cs`, + actualiza `SpaContextModelSnapshot.cs`)

**Interfaces:**
- Produces: `Role.TenantId` (`string?`), `Role.RutaPorDefecto` (`string?`), `Role.Prioridad` (`int`, default 100 en BD); `RoleSubmodule { RoleId: string, SubmoduleId: string }`; `SpaContext.RoleSubmodule` (`DbSet<RoleSubmodule>`).

- [ ] **Step 1: Editar `Role.cs`**

Reemplazar el contenido completo de `Backend/Domain/Entities/Identity/Role.cs`:

```csharp

using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;

public class Role : IdentityRole<string>
{
    public string? TenantId { get; set; }
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; }

    public List<UserRol> UserRoles { get; set; } = new List<UserRol>();
    public List<RoleSubmodule> RoleSubmodules { get; set; } = new List<RoleSubmodule>();
}
```

- [ ] **Step 2: Crear `RoleSubmodule.cs`**

```csharp
namespace Domain.Entities.Identity;

public class RoleSubmodule : EntityBase
{
    public string RoleId { get; set; } = null!;
    public string SubmoduleId { get; set; } = null!;

    public Role Role { get; set; } = null!;
    public AspNetSubModule Submodule { get; set; } = null!;
}
```

(No hace falta `using Domain.Entities;`: `EntityBase` vive en `Domain.Entities`, y `Domain.Entities.Identity` es un namespace anidado dentro de ese — C# resuelve tipos del namespace contenedor sin `using` explícito. Mismo motivo por el que `AspNetUserSubModule.cs`, en la misma carpeta, extiende `EntityBase` sin ningún `using` al principio del archivo.)

- [ ] **Step 3: Reescribir `RoleConfiguration.cs`**

Reemplazar el contenido completo de `Backend/Infrastructure/Configuration/RoleConfiguration.cs`:

```csharp
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Configuration
{
    public class RoleConfiguration
    {
        public RoleConfiguration(EntityTypeBuilder<Role> entityBuilder)
        {
            entityBuilder.HasKey(x => x.Id);

            entityBuilder.Property(x => x.Prioridad).HasDefaultValue(100);

            entityBuilder.HasMany(e => e.UserRoles)
                         .WithOne(e => e.Role)
                         .HasForeignKey(e => e.RoleId)
                         .OnDelete(DeleteBehavior.Restrict)
                         .IsRequired();

            entityBuilder.HasMany(e => e.RoleSubmodules)
                         .WithOne(e => e.Role)
                         .HasForeignKey(e => e.RoleId)
                         .OnDelete(DeleteBehavior.Cascade);

            // El indice unico por defecto de ASP.NET Identity (RoleNameIndex) es solo sobre
            // NormalizedName: dos tenants distintos no podrian tener cada uno un rol "Ventas".
            // Se neutraliza y se reemplaza por uno compuesto con TenantId (TenantId NULL =
            // fila global, ej. SuperAdmin, que sigue siendo unica en todo el sistema).
            entityBuilder.HasIndex(x => x.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique(false);
            entityBuilder.HasIndex(x => new { x.NormalizedName, x.TenantId }).HasDatabaseName("RoleNameTenantIndex").IsUnique();
        }
    }
}
```

- [ ] **Step 4: Crear `RoleSubmoduleConfiguration.cs`**

```csharp
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class RoleSubmoduleConfiguration
    {
        public RoleSubmoduleConfiguration(EntityTypeBuilder<RoleSubmodule> entityBuilder)
        {
            entityBuilder.HasKey(e => new { e.RoleId, e.SubmoduleId });

            entityBuilder.HasOne(d => d.Role)
                         .WithMany(p => p.RoleSubmodules)
                         .HasForeignKey(d => d.RoleId)
                         .OnDelete(DeleteBehavior.Cascade);

            entityBuilder.HasOne(d => d.Submodule)
                         .WithMany()
                         .HasForeignKey(d => d.SubmoduleId)
                         .OnDelete(DeleteBehavior.Restrict);

            entityBuilder.Ignore(e => e.Id);
        }
    }
}
```

Nota: quitar `.HasMany(e => e.RoleSubmodules).WithOne(e => e.Role)...` del Step 3 de `RoleConfiguration.cs` de la relación FK — ya se define completa acá en `RoleSubmoduleConfiguration` con `HasOne(d => d.Role).WithMany(p => p.RoleSubmodules)`. Definir la misma relación en los dos lados causa error de EF Core ("ya existe una relación configurada"). Dejar SOLO la de `RoleSubmoduleConfiguration.cs`; en `RoleConfiguration.cs` del Step 3, borrar este bloque:

```csharp
            entityBuilder.HasMany(e => e.RoleSubmodules)
                         .WithOne(e => e.Role)
                         .HasForeignKey(e => e.RoleId)
                         .OnDelete(DeleteBehavior.Cascade);
```

(Es decir: el Step 3 de arriba, tal como está escrito, NO debe incluir ese bloque. `RoleConfiguration.cs` final solo tiene el `HasKey`, el `Prioridad` default, el `HasMany(UserRoles)`, y los dos `HasIndex`.)

- [ ] **Step 5: Editar `SpaContext.cs` — DbSet nuevo**

En `Backend/Infrastructure/Data/SpaContext.cs`, buscar la línea:

```csharp
    public DbSet<AspNetUserSubModule> AspNetUserSubModule => Set<AspNetUserSubModule>();
```

Agregar inmediatamente después:

```csharp
    public DbSet<RoleSubmodule> RoleSubmodule => Set<RoleSubmodule>();
```

- [ ] **Step 6: Editar `SpaContext.cs` — query filters**

Buscar esta línea (dentro de `OnModelCreating`, cerca de las otras `HasQueryFilter`):

```csharp
        modelBuilder.Entity<AspNetUserSubModule>().HasQueryFilter(e => e.TenantId == _tenant.Name);
```

Agregar inmediatamente después:

```csharp
        modelBuilder.Entity<Role>().HasQueryFilter(e => e.TenantId == null || e.TenantId == _tenant.Name);
        modelBuilder.Entity<RoleSubmodule>().HasQueryFilter(e => e.TenantId == _tenant.Name);
```

- [ ] **Step 7: Editar `SpaContext.cs` — aplicar la configuración nueva**

Buscar la línea:

```csharp
        new UserSubmoduleConfiguration(modelBuilder.Entity<AspNetUserSubModule>());
```

Agregar inmediatamente después:

```csharp
        new RoleSubmoduleConfiguration(modelBuilder.Entity<RoleSubmodule>());
```

(`RoleConfiguration` ya se aplica un poco antes, en `new RoleConfiguration(modelBuilder.Entity<Role>());` — no tocar esa línea, ya existe.)

- [ ] **Step 8: Generar la migración**

Run:
```bash
cd Backend
dotnet ef migrations add AddRoleTenantAndRoleSubmodule --project Infrastructure/Infrastructure.csproj --startup-project WEB_API/Spa.Api.csproj --context SpaContext
```

Expected: `Done. To undo this action, use 'ef migrations remove'` y aparecen dos archivos nuevos en `Backend/Infrastructure/Migrations/`.

- [ ] **Step 9: Verificar el contenido de la migración generada**

Abrir el archivo `Backend/Infrastructure/Migrations/<timestamp>_AddRoleTenantAndRoleSubmodule.cs` y confirmar que el método `Up` contiene, en este orden aproximado:
1. `migrationBuilder.AddColumn<string>(name: "TenantId", table: "AspNetRoles", ...)`.
2. `migrationBuilder.AddColumn<string>(name: "RutaPorDefecto", table: "AspNetRoles", ...)`.
3. `migrationBuilder.AddColumn<int>(name: "Prioridad", table: "AspNetRoles", ..., defaultValue: 100)`.
4. `migrationBuilder.DropIndex(name: "RoleNameIndex", table: "AspNetRoles")` seguido de `migrationBuilder.CreateIndex(name: "RoleNameIndex", table: "AspNetRoles", column: "NormalizedName")` **sin** `unique: true` — si en cambio ves que `RoleNameIndex` sigue con `unique: true`, el Step 3 no se aplicó correctamente: revisar que `RoleConfiguration.cs` tenga las dos líneas `HasIndex` exactas del Step 3.
5. `migrationBuilder.CreateIndex(name: "RoleNameTenantIndex", table: "AspNetRoles", columns: new[] { "NormalizedName", "TenantId" }, unique: true)`.
6. `migrationBuilder.CreateTable(name: "RoleSubmodule", ...)` con columnas `RoleId`, `SubmoduleId`, `TenantId`, `UsuarioCreacion`, `FechaCreacion`, `Estado`, y una PK compuesta `RoleId, SubmoduleId`.

Si falta el `DropIndex`/`CreateIndex` de `RoleNameIndex` sin unique (punto 4), NO seguir: borrar la migración (`dotnet ef migrations remove --project Infrastructure/Infrastructure.csproj --startup-project WEB_API/Spa.Api.csproj --context SpaContext`), revisar `RoleConfiguration.cs`, y repetir el Step 8.

- [ ] **Step 10: Compilar**

Run: `cd Backend && dotnet build --nologo -v quiet 2>&1 | grep -i error`
Expected: `0 Errores`.

- [ ] **Step 11: Commit**

```bash
cd Backend
git add Domain/Entities/Identity/Role.cs Domain/Entities/Identity/RoleSubmodule.cs \
        Infrastructure/Configuration/RoleConfiguration.cs Infrastructure/Configuration/RoleSubmoduleConfiguration.cs \
        Infrastructure/Data/SpaContext.cs Infrastructure/Migrations/
git commit -m "feat: modelo de datos para roles por tenant (Role + RoleSubmodule)

Role gana TenantId (nullable, NULL = global como SuperAdmin),
RutaPorDefecto y Prioridad. Tabla RoleSubmodule reemplaza la
asignacion directa Usuario-Submodulo. Indice unico de nombre de rol
pasa a ser compuesto con TenantId, para que dos tenants puedan tener
cada uno un rol 'Ventas' sin chocar."
```

---

## Task 3: IRoleRepository + RoleRepository (CRUD, catálogo, resolución de acceso)

**Files:**
- Create: `Backend/Domain/DTO/RoleDto.cs`
- Create: `Backend/Domain/Payloads/RolePayloads.cs`
- Create: `Backend/Application/Interfaces/IRepository/IRoleRepository.cs`
- Create: `Backend/Infrastructure/Repositories/RoleRepository.cs`
- Test: `Backend/Tests/Infrastructure.Tests/RoleRepositoryTests.cs`

**Interfaces:**
- Consumes: `Domain.DTO.AccesosDetalle` / `Domain.DTO.SubModuloDetalle` (ya existen en `Domain/DTO/ApplicationUserDto.cs`, sin cambios). `Infrastructure.Tests.TestDbContextFactory` / `FakeTenantResolver` (ya existen).
- Produces: `IRoleRepository` con los métodos listados abajo — Task 4 (`RoleService`) y Task 5 (`AuthenticationRepository`) dependen de estas firmas exactas.

- [ ] **Step 1: Crear `RoleDto.cs`**

```csharp
namespace Domain.DTO;

public class RoleDto
{
    public string Id { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; }
    public int CantidadUsuarios { get; set; }
    public List<string> SubmoduleIds { get; set; } = new();
}
```

- [ ] **Step 2: Crear `RolePayloads.cs`**

```csharp
namespace Domain.Payloads;

public class CreateRolePayload
{
    public string Nombre { get; set; } = null!;
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; } = 100;
    public List<string> SubmoduleIds { get; set; } = new();
}

public class UpdateRolePayload
{
    public string Nombre { get; set; } = null!;
    public string? RutaPorDefecto { get; set; }
    public int Prioridad { get; set; } = 100;
    public List<string> SubmoduleIds { get; set; } = new();
}

public class AsignarRolesUsuarioPayload
{
    public string UserId { get; set; } = null!;
    public List<string> RoleIds { get; set; } = new();
}
```

- [ ] **Step 3: Crear `IRoleRepository.cs`**

```csharp
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IRoleRepository
{
    Task<(ServiceStatus, List<RoleDto>?, string)> ListarRoles();
    Task<(ServiceStatus, RoleDto?, string)> ObtenerRol(string id);
    Task<(ServiceStatus, RoleDto?, string)> CrearRol(CreateRolePayload payload);
    Task<(ServiceStatus, RoleDto?, string)> ActualizarRol(string id, UpdateRolePayload payload);
    Task<(ServiceStatus, string)> EliminarRol(string id);
    Task<(ServiceStatus, List<AccesosDetalle>?, string)> ObtenerCatalogoSubmodulos();
    Task<(ServiceStatus, string)> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload);
    Task<(List<AccesosDetalle> Rutas, string? RutaPorDefecto)> ResolverAccesoUsuario(IList<string> nombresDeRol);
}
```

- [ ] **Step 4: Crear `RoleRepository.cs`**

```csharp
using Domain.DTO;
using Domain.Entities.Identity;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Application.Interfaces.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly SpaContext _context;
    private readonly RoleManager<Role> _roleManager;

    public RoleRepository(SpaContext context, RoleManager<Role> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<(ServiceStatus, List<RoleDto>?, string)> ListarRoles()
    {
        try
        {
            var roles = await _context.Roles.AsNoTracking()
                                             .Where(r => r.TenantId == _context.CurrentTenantName)
                                             .OrderBy(r => r.Prioridad)
                                             .ToListAsync();

            var lista = new List<RoleDto>();

            foreach (var role in roles)
            {
                var submoduleIds = await _context.RoleSubmodule.AsNoTracking()
                                                                .Where(rs => rs.RoleId == role.Id)
                                                                .Select(rs => rs.SubmoduleId)
                                                                .ToListAsync();

                var cantidadUsuarios = await _context.UserRoles.AsNoTracking()
                                                                .CountAsync(ur => ur.RoleId == role.Id);

                lista.Add(new RoleDto
                {
                    Id = role.Id,
                    Nombre = role.Name,
                    RutaPorDefecto = role.RutaPorDefecto,
                    Prioridad = role.Prioridad,
                    CantidadUsuarios = cantidadUsuarios,
                    SubmoduleIds = submoduleIds
                });
            }

            return (ServiceStatus.Ok, lista, "Roles listados correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar roles -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, RoleDto?, string)> ObtenerRol(string id)
    {
        try
        {
            var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
                return (ServiceStatus.NotFound, null, $"No se encontro el rol {id}");

            var submoduleIds = await _context.RoleSubmodule.AsNoTracking()
                                                            .Where(rs => rs.RoleId == role.Id)
                                                            .Select(rs => rs.SubmoduleId)
                                                            .ToListAsync();

            var cantidadUsuarios = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == role.Id);

            var dto = new RoleDto
            {
                Id = role.Id,
                Nombre = role.Name,
                RutaPorDefecto = role.RutaPorDefecto,
                Prioridad = role.Prioridad,
                CantidadUsuarios = cantidadUsuarios,
                SubmoduleIds = submoduleIds
            };

            return (ServiceStatus.Ok, dto, "Rol encontrado");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, RoleDto?, string)> CrearRol(CreateRolePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Nombre))
            return (ServiceStatus.FailedValidation, null, "El nombre del rol es obligatorio");

        var yaExiste = await _context.Roles.AsNoTracking()
                                            .AnyAsync(r => r.TenantId == _context.CurrentTenantName
                                                        && r.NormalizedName == payload.Nombre.ToUpper());

        if (yaExiste)
            return (ServiceStatus.FailedValidation, null, $"Ya existe un rol llamado '{payload.Nombre}'");

        try
        {
            var role = new Role
            {
                Name = payload.Nombre,
                TenantId = _context.CurrentTenantName,
                RutaPorDefecto = payload.RutaPorDefecto,
                Prioridad = payload.Prioridad
            };

            var resultado = await _roleManager.CreateAsync(role);

            if (!resultado.Succeeded)
                return (ServiceStatus.FailedValidation, null, string.Join("; ", resultado.Errors.Select(e => e.Description)));

            foreach (var submoduleId in payload.SubmoduleIds.Distinct())
            {
                _context.RoleSubmodule.Add(new RoleSubmodule { RoleId = role.Id, SubmoduleId = submoduleId });
            }

            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, new RoleDto
            {
                Id = role.Id,
                Nombre = role.Name,
                RutaPorDefecto = role.RutaPorDefecto,
                Prioridad = role.Prioridad,
                CantidadUsuarios = 0,
                SubmoduleIds = payload.SubmoduleIds.Distinct().ToList()
            }, "Rol creado correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al crear el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, RoleDto?, string)> ActualizarRol(string id, UpdateRolePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Nombre))
            return (ServiceStatus.FailedValidation, null, "El nombre del rol es obligatorio");

        var role = await _context.Roles.AsTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return (ServiceStatus.NotFound, null, $"No se encontro el rol {id}");

        var nombreEnUso = await _context.Roles.AsNoTracking()
                                               .AnyAsync(r => r.Id != id
                                                           && r.TenantId == _context.CurrentTenantName
                                                           && r.NormalizedName == payload.Nombre.ToUpper());

        if (nombreEnUso)
            return (ServiceStatus.FailedValidation, null, $"Ya existe un rol llamado '{payload.Nombre}'");

        try
        {
            role.Name = payload.Nombre;
            role.NormalizedName = payload.Nombre.ToUpper();
            role.RutaPorDefecto = payload.RutaPorDefecto;
            role.Prioridad = payload.Prioridad;

            var existentes = _context.RoleSubmodule.Where(rs => rs.RoleId == id);
            _context.RoleSubmodule.RemoveRange(existentes);

            foreach (var submoduleId in payload.SubmoduleIds.Distinct())
            {
                _context.RoleSubmodule.Add(new RoleSubmodule { RoleId = id, SubmoduleId = submoduleId });
            }

            await _context.SaveChangesAsync();

            var cantidadUsuarios = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == id);

            return (ServiceStatus.Ok, new RoleDto
            {
                Id = role.Id,
                Nombre = role.Name,
                RutaPorDefecto = role.RutaPorDefecto,
                Prioridad = role.Prioridad,
                CantidadUsuarios = cantidadUsuarios,
                SubmoduleIds = payload.SubmoduleIds.Distinct().ToList()
            }, "Rol actualizado correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al actualizar el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> EliminarRol(string id)
    {
        var role = await _context.Roles.AsTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return (ServiceStatus.NotFound, $"No se encontro el rol {id}");

        var cantidadUsuarios = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == id);

        if (cantidadUsuarios > 0)
            return (ServiceStatus.FailedValidation, $"No se puede eliminar: el rol tiene {cantidadUsuarios} usuario(s) asignado(s)");

        try
        {
            var submodulos = _context.RoleSubmodule.Where(rs => rs.RoleId == id);
            _context.RoleSubmodule.RemoveRange(submodulos);
            await _context.SaveChangesAsync();

            var resultado = await _roleManager.DeleteAsync(role);

            if (!resultado.Succeeded)
                return (ServiceStatus.FailedValidation, string.Join("; ", resultado.Errors.Select(e => e.Description)));

            return (ServiceStatus.Ok, "Rol eliminado correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al eliminar el rol -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, List<AccesosDetalle>?, string)> ObtenerCatalogoSubmodulos()
    {
        try
        {
            var submodulos = await _context.AspNetSubModule.AsNoTracking()
                                                            .Include(s => s.Module)
                                                            .ToListAsync();

            var agrupado = submodulos.GroupBy(s => s.ModuloId)
                                      .Select(g => new AccesosDetalle
                                      {
                                          Modulo = g.Key,
                                          ModuloNombre = g.First().Module.Nombre,
                                          SubModulos = g.Select(s => new SubModuloDetalle
                                          {
                                              SubModulo = s.Identificador,
                                              SubModuloNombre = s.Nombre
                                          }).ToList()
                                      })
                                      .OrderBy(g => g.Modulo)
                                      .ToList();

            return (ServiceStatus.Ok, agrupado, "Catalogo de submodulos");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener el catalogo -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload)
    {
        var usuario = await _context.Users.AsNoTracking()
                                          .FirstOrDefaultAsync(u => u.Id == payload.UserId
                                                                  && u.TenantId.ToString() == _context.CurrentTenantName);

        if (usuario == null)
            return (ServiceStatus.NotFound, $"No se encontro el usuario {payload.UserId}");

        try
        {
            var roles = await _context.Roles.AsNoTracking()
                                             .Where(r => payload.RoleIds.Contains(r.Id))
                                             .Select(r => r.Id)
                                             .ToListAsync();

            var existentes = _context.UserRoles.Where(ur => ur.UserId == payload.UserId);
            _context.UserRoles.RemoveRange(existentes);
            await _context.SaveChangesRegularAsync();

            foreach (var roleId in roles)
            {
                _context.UserRoles.Add(new UserRol { UserId = payload.UserId, RoleId = roleId });
            }

            await _context.SaveChangesRegularAsync();

            return (ServiceStatus.Ok, "Roles asignados correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al asignar roles -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(List<AccesosDetalle> Rutas, string? RutaPorDefecto)> ResolverAccesoUsuario(IList<string> nombresDeRol)
    {
        if (nombresDeRol == null || nombresDeRol.Count == 0)
            return (new List<AccesosDetalle>(), null);

        var roles = await _context.Roles.AsNoTracking()
                                         .Where(r => nombresDeRol.Contains(r.Name))
                                         .OrderBy(r => r.Prioridad)
                                         .ToListAsync();

        var rutaPorDefecto = roles.FirstOrDefault()?.RutaPorDefecto;

        var roleIds = roles.Select(r => r.Id).ToList();

        var submoduleIds = await _context.RoleSubmodule.AsNoTracking()
                                                        .Where(rs => roleIds.Contains(rs.RoleId))
                                                        .Select(rs => rs.SubmoduleId)
                                                        .Distinct()
                                                        .ToListAsync();

        var submodulos = await _context.AspNetSubModule.AsNoTracking()
                                                        .Include(s => s.Module)
                                                        .Where(s => submoduleIds.Contains(s.Identificador))
                                                        .ToListAsync();

        var rutas = submodulos.GroupBy(s => s.ModuloId)
                               .Select(g => new AccesosDetalle
                               {
                                   Modulo = g.Key,
                                   ModuloNombre = g.First().Module.Nombre,
                                   SubModulos = g.Select(s => new SubModuloDetalle
                                   {
                                       SubModulo = s.Identificador,
                                       SubModuloNombre = s.Nombre
                                   }).ToList()
                               })
                               .ToList();

        return (rutas, rutaPorDefecto);
    }
}
```

Nota sobre `AsignarRolesAUsuario`: usa `_context.SaveChangesRegularAsync()` (no el `SaveChangesAsync` sobreescrito) al tocar `UserRol`/`AspNetUserRoles` porque esa tabla es de ASP.NET Identity puro (`IdentityUserRole<string>`), no extiende `EntityBase`, y no necesita el procesamiento de auditoria/TenantId automatico que hace el override — el mismo patron que ya usa `TenantRepository.CrearSucursal` para no pisar el tenant ambiente. Verificar que `SaveChangesRegularAsync` existe en `SpaContext` (ya se uso en sesiones anteriores, deberia estar definido cerca del `SaveChangesAsync` sobreescrito).

También nota: `u.TenantId.ToString() == _context.CurrentTenantName` — revisar el tipo real de `User.TenantId` (es `int` según `Domain/Entities/Identity/User.cs`, mientras que `CurrentTenantName` es el `Tenant.Name` en formato string, ej. "SPASOLIS1"). Si `User.TenantId` (int) no es directamente comparable contra el nombre del tenant, reemplazar ese filtro por `u.Id == payload.UserId` únicamente (sin el segundo filtro) — la query filter global de `Users` ya viene scopeada indirectamente al no usar `IgnoreQueryFilters()`; confirmar mirando `SpaContext.OnModelCreating` si `User`/`AspNetUsers` tiene un `HasQueryFilter` por `TenantId` (si lo tiene, el segundo filtro es redundante y se puede borrar; si no lo tiene, hay que resolverlo comparando `u.TenantId` contra el `Identificador` (int) del tenant actual, no contra `CurrentTenantName` (string) — buscar cómo lo hace `UsersRepository.ListarUsuarios`, que sí filtra usuarios por tenant, y copiar ese mismo criterio exacto).

- [ ] **Step 5: Escribir los tests**

Crear `Backend/Tests/Infrastructure.Tests/RoleRepositoryTests.cs`:

```csharp
using Domain.Entities.Identity;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class RoleRepositoryTests
{
    private static async Task SeedCatalogoAsync(Infrastructure.Data.SpaContext context)
    {
        context.AspNetModule.Add(new AspNetModule { Identificador = "200", Nombre = "Productos" });
        context.AspNetSubModule.Add(new AspNetSubModule { Identificador = "201", Nombre = "Productos", ModuloId = "200" });
        context.AspNetModule.Add(new AspNetModule { Identificador = "300", Nombre = "Ventas" });
        context.AspNetSubModule.Add(new AspNetSubModule { Identificador = "301", Nombre = "Ventas", ModuloId = "300" });
        await context.SaveChangesAsync();
    }

    private static RoleManager<Role> BuildRoleManager(Infrastructure.Data.SpaContext context)
    {
        var store = new RoleStore<Role, Infrastructure.Data.SpaContext, string>(context);
        return new RoleManager<Role>(store, new IRoleValidator<Role>[] { new RoleValidator<Role>() }, null!, null!, null!);
    }

    [Fact]
    public async Task CrearRol_AsignaSubmodulosYQuedaVisibleEnListarRoles()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));

        var (estado, creado, _) = await repo.CrearRol(new CreateRolePayload
        {
            Nombre = "Ventas",
            RutaPorDefecto = "/facturacion",
            Prioridad = 20,
            SubmoduleIds = new List<string> { "301" }
        });

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Equal(new List<string> { "301" }, creado!.SubmoduleIds);

        var (estadoLista, lista, _) = await repo.ListarRoles();
        Assert.Equal(ServiceStatus.Ok, estadoLista);
        Assert.Single(lista!);
        Assert.Equal("Ventas", lista![0].Nombre);
    }

    [Fact]
    public async Task CrearRol_NombreDuplicadoEnElMismoTenant_Falla()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));

        await repo.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });
        var (estado, _, mensaje) = await repo.CrearRol(new CreateRolePayload { Nombre = "ventas", Prioridad = 30 });

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("Ya existe", mensaje);
    }

    [Fact]
    public async Task DosTenants_PuedenTenerCadaUnoUnRolConElMismoNombre()
    {
        var (contextA, connectionA) = TestDbContextFactory.CreateContext(new FakeTenantResolver(tenantName: "TENANT_A"));
        using var __ = connectionA;
        await SeedCatalogoAsync(contextA);
        var repoA = new RoleRepository(contextA, BuildRoleManager(contextA));
        var (estadoA, _, _) = await repoA.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });

        var (contextB, connectionB) = TestDbContextFactory.CreateContext(new FakeTenantResolver(tenantName: "TENANT_B"));
        using var ___ = connectionB;
        await SeedCatalogoAsync(contextB);
        var repoB = new RoleRepository(contextB, BuildRoleManager(contextB));
        var (estadoB, _, _) = await repoB.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });

        Assert.Equal(ServiceStatus.Ok, estadoA);
        Assert.Equal(ServiceStatus.Ok, estadoB);
    }

    [Fact]
    public async Task EliminarRol_ConUsuariosAsignados_Falla()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));
        var (_, creado, _) = await repo.CrearRol(new CreateRolePayload { Nombre = "Ventas", Prioridad = 20 });

        context.UserRoles.Add(new UserRol { UserId = "user-1", RoleId = creado!.Id });
        await context.SaveChangesRegularAsync();

        var (estado, mensaje) = await repo.EliminarRol(creado.Id);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("1 usuario", mensaje);
    }

    [Fact]
    public async Task ResolverAccesoUsuario_ConDosRoles_UneSubmodulosYGanaLaMenorPrioridad()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;
        await SeedCatalogoAsync(context);

        var repo = new RoleRepository(context, BuildRoleManager(context));

        await repo.CrearRol(new CreateRolePayload { Nombre = "Ventas", RutaPorDefecto = "/facturacion", Prioridad = 20, SubmoduleIds = new List<string> { "301" } });
        await repo.CrearRol(new CreateRolePayload { Nombre = "Admin", RutaPorDefecto = "/dashboard/productos", Prioridad = 0, SubmoduleIds = new List<string> { "201", "301" } });

        var (rutas, rutaPorDefecto) = await repo.ResolverAccesoUsuario(new List<string> { "Ventas", "Admin" });

        Assert.Equal("/dashboard/productos", rutaPorDefecto); // gana Admin (prioridad 0)
        Assert.Equal(2, rutas.Sum(r => r.SubModulos.Count)); // 201 + 301, sin duplicar
    }

    [Fact]
    public async Task ResolverAccesoUsuario_SinRoles_DevuelveVacioSinRutaPorDefecto()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var repo = new RoleRepository(context, BuildRoleManager(context));

        var (rutas, rutaPorDefecto) = await repo.ResolverAccesoUsuario(new List<string>());

        Assert.Empty(rutas);
        Assert.Null(rutaPorDefecto);
    }
}
```

- [ ] **Step 6: Correr los tests**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --nologo --filter "RoleRepositoryTests" -v normal 2>&1 | tail -30`
Expected: los 6 tests en verde. Si `BuildRoleManager` falla al construirse (los `null!` en `RoleManager<Role>` pueden requerir `ILogger<RoleManager<Role>>` no nulo dependiendo de la version de Identity), reemplazar el ultimo `null!` por `new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoleManager<Role>>()` y volver a correr.

- [ ] **Step 7: Compilar toda la solución**

Run: `cd Backend && dotnet build WEB_API_SPA.sln --nologo -v quiet 2>&1 | grep -i error`
Expected: `0 Errores`.

- [ ] **Step 8: Commit**

```bash
cd Backend
git add Domain/DTO/RoleDto.cs Domain/Payloads/RolePayloads.cs \
        Application/Interfaces/IRepository/IRoleRepository.cs Infrastructure/Repositories/RoleRepository.cs \
        Tests/Infrastructure.Tests/RoleRepositoryTests.cs
git commit -m "feat: RoleRepository - CRUD de roles, catalogo y resolucion de acceso

ResolverAccesoUsuario centraliza el calculo de rutas/rutaPorDefecto a
partir de los roles de un usuario; lo va a consumir tanto el login
(Task 5) como, a futuro, cualquier otro lugar que necesite lo mismo."
```

---

## Task 4: IRoleService + RoleService + RolesController + registro en DI

**Files:**
- Create: `Backend/Application/Interfaces/IServices/IRoleService.cs`
- Create: `Backend/Application/Services/RoleService.cs`
- Create: `Backend/WEB_API/Controllers/RolesController.cs`
- Modify: `Backend/Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IRoleRepository` (Task 3).
- Produces: endpoints `GET/POST/PUT/DELETE /api/roles`, `GET /api/roles/catalogo-submodulos`, `PUT /api/roles/asignar-usuario` — el frontend (Task 15/16) llama exactamente estas rutas.

- [ ] **Step 1: Crear `IRoleService.cs`**

```csharp
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IRoleService
{
    Task<MessageResult<object>> ListarRoles();
    Task<MessageResult<object>> ObtenerRol(string id);
    Task<MessageResult<object>> CrearRol(CreateRolePayload payload);
    Task<MessageResult<object>> ActualizarRol(string id, UpdateRolePayload payload);
    Task<MessageResult<bool>> EliminarRol(string id);
    Task<MessageResult<object>> ObtenerCatalogoSubmodulos();
    Task<MessageResult<bool>> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload);
}
```

- [ ] **Step 2: Crear `RoleService.cs`**

```csharp
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<MessageResult<object>> ListarRoles()
    {
        var (estado, result, message) = await _roleRepository.ListarRoles();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ObtenerRol(string id)
    {
        var (estado, result, message) = await _roleRepository.ObtenerRol(id);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> CrearRol(CreateRolePayload payload)
    {
        var (estado, result, message) = await _roleRepository.CrearRol(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ActualizarRol(string id, UpdateRolePayload payload)
    {
        var (estado, result, message) = await _roleRepository.ActualizarRol(id, payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<bool>> EliminarRol(string id)
    {
        var (estado, message) = await _roleRepository.EliminarRol(id);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }

    public async Task<MessageResult<object>> ObtenerCatalogoSubmodulos()
    {
        var (estado, result, message) = await _roleRepository.ObtenerCatalogoSubmodulos();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<bool>> AsignarRolesAUsuario(AsignarRolesUsuarioPayload payload)
    {
        var (estado, message) = await _roleRepository.AsignarRolesAUsuario(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound ? HttpStatusCode.NotFound : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }
}
```

- [ ] **Step 3: Crear `RolesController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/roles")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> ListarRoles() => Ok(await _roleService.ListarRoles());

    [HttpGet("catalogo-submodulos")]
    public async Task<IActionResult> ObtenerCatalogoSubmodulos() => Ok(await _roleService.ObtenerCatalogoSubmodulos());

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerRol(string id) => Ok(await _roleService.ObtenerRol(id));

    [HttpPost]
    public async Task<IActionResult> CrearRol([FromBody] CreateRolePayload payload) => Ok(await _roleService.CrearRol(payload));

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarRol(string id, [FromBody] UpdateRolePayload payload) => Ok(await _roleService.ActualizarRol(id, payload));

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarRol(string id) => Ok(await _roleService.EliminarRol(id));

    [HttpPut("asignar-usuario")]
    public async Task<IActionResult> AsignarRolesAUsuario([FromBody] AsignarRolesUsuarioPayload payload) => Ok(await _roleService.AsignarRolesAUsuario(payload));
}
```

- [ ] **Step 4: Registrar en `DependencyInjection.cs`**

En `Backend/Infrastructure/DependencyInjection.cs`, en el primer bloque `services.AddScoped<...>` (donde está `.AddScoped<IDashboardRepository, DashboardRepository>()`), agregar antes del `;` final del bloque:

```csharp
                    .AddScoped<IRoleRepository, RoleRepository>()
```

Y en el segundo bloque (donde está `.AddScoped<IDashboardService, DashboardService>()`), agregar en el mismo lugar:

```csharp
                    .AddScoped<IRoleService, RoleService>()
```

- [ ] **Step 5: Compilar**

Run: `cd Backend && dotnet build --nologo -v quiet 2>&1 | grep -i error`
Expected: `0 Errores`.

- [ ] **Step 6: Probar manualmente con el servidor local**

Run: `cd Backend/WEB_API && dotnet run --urls http://localhost:5080` (en background si hace falta seguir usando la terminal).

Con el servidor arriba y un token válido (usar el mismo flujo que ya usás para probar otros endpoints, ej. Swagger en `http://localhost:5080`), probar `GET /api/roles/catalogo-submodulos` y confirmar que devuelve el árbol de módulos/submódulos sembrados en la base local. Frenar el servidor después (`Ctrl+C` o matar el proceso).

- [ ] **Step 7: Commit**

```bash
cd Backend
git add Application/Interfaces/IServices/IRoleService.cs Application/Services/RoleService.cs \
        WEB_API/Controllers/RolesController.cs Infrastructure/DependencyInjection.cs
git commit -m "feat: API de roles (CRUD, catalogo de submodulos, asignar a usuario)"
```

---

## Task 5: Login emite roles/rutaPorDefecto en el JWT

**Files:**
- Modify: `Backend/Infrastructure/Repositories/AuthenticationRepository.cs`
- Modify: `Backend/WEB_API/Authentication/ClaimsPrincipalExtension.cs`

No se agrega un test de repositorio nuevo para este task a propósito: `AuthenticationRepository` requiere `SignInManager<User>`/`UserManager<User>`/`TokenValidationParameters` reales o mockeados para ejercitar `Token()` de punta a punta, un esfuerzo de mocking desproporcionado para un cambio que es, en los hechos, un pass-through de una línea hacia `IRoleRepository.ResolverAccesoUsuario` — ya cubierto por los tests de `RoleRepositoryTests` (Task 3). Este task se valida con la prueba manual del Step 6.

**Interfaces:**
- Consumes: `IRoleRepository.ResolverAccesoUsuario` (Task 3).
- Produces: claim JWT `"rutaPorDefecto"` (string, puede no estar presente); `AuthenticatedUser.Roles` (`List<string>`), `AuthenticatedUser.RutaPorDefecto` (`string?`) — el frontend (Task 9) lee estos dos campos de `GET /api/autenticacion/@me`.

- [ ] **Step 1: Inyectar `IRoleRepository` en `AuthenticationRepository`**

En `Backend/Infrastructure/Repositories/AuthenticationRepository.cs`, agregar el campo y el parámetro de constructor. Buscar:

```csharp
        private readonly IMapper _mapper;
        private readonly ILogger<AuthenticationRepository> _logger;

        public AuthenticationRepository(
            SpaContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IOptions<TokenManagement> tokenSettings,
            TokenValidationParameters tokenValidationParameters,
            IMapper mapper,
            ILogger<AuthenticationRepository> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenSettings = tokenSettings.Value;
            _tokenValidationParameters = tokenValidationParameters;
```

Reemplazar por:

```csharp
        private readonly IMapper _mapper;
        private readonly ILogger<AuthenticationRepository> _logger;
        private readonly Application.Interfaces.IRepository.IRoleRepository _roleRepository;

        public AuthenticationRepository(
            SpaContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IOptions<TokenManagement> tokenSettings,
            TokenValidationParameters tokenValidationParameters,
            IMapper mapper,
            ILogger<AuthenticationRepository> logger,
            Application.Interfaces.IRepository.IRoleRepository roleRepository)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenSettings = tokenSettings.Value;
            _tokenValidationParameters = tokenValidationParameters;
            _roleRepository = roleRepository;
```

(Revisar que el resto del cuerpo del constructor original — asignaciones de `_mapper`, `_logger`, etc. — se mantenga sin cambios; el bloque de arriba solo muestra las líneas que cambian, no todo el constructor.)

- [ ] **Step 2: Reemplazar el cálculo de `rutas` en `Token()`**

Buscar, dentro del método `Token(...)`:

```csharp
                    var applicationUserDto = _mapper.Map<ApplicationUserDto>(user);

                    var rutas = applicationUserDto.Resumen;

                    claims.Add(new Claim("rutas", Newtonsoft.Json.JsonConvert.SerializeObject(rutas)));
```

Reemplazar por:

```csharp
                    var (rutas, rutaPorDefecto) = await _roleRepository.ResolverAccesoUsuario(userRoles);

                    claims.Add(new Claim("rutas", Newtonsoft.Json.JsonConvert.SerializeObject(rutas)));

                    if (!string.IsNullOrEmpty(rutaPorDefecto))
                        claims.Add(new Claim("rutaPorDefecto", rutaPorDefecto));
```

Esto depende de que `userRoles` (la lista de nombres de rol) ya esté calculada más arriba en el mismo método, en este bloque que NO cambia:

```csharp
                    if (_userManager.SupportsUserRole)
                    {
                        IList<string> userRoles = await _userManager.GetRolesAsync(user);
                        foreach (string role in userRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }
```

Si `userRoles` queda declarado dentro del `if` y no es visible más abajo (error de compilación "no existe en el contexto actual"), mover la declaración `IList<string> userRoles` fuera del `if`, inicializada en `new List<string>()`, y dejar el `foreach` adentro del `if` igual. Ejemplo:

```csharp
                    IList<string> userRoles = new List<string>();

                    if (_userManager.SupportsUserRole)
                    {
                        userRoles = await _userManager.GetRolesAsync(user);
                        foreach (string role in userRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }
```

- [ ] **Step 3: Quitar el `Include` de submódulos ya innecesario**

En el mismo archivo, dentro de `FindByUsername(string username)`, buscar:

```csharp
            var appUser = await _context.Users.AsTracking()
                                              .IgnoreQueryFilters()//SE AÑADIO ULTIMO
                                              .Include(p => p.Tenant)
                                              .Include(p => p.UserSubmodules).ThenInclude(p => p.Submodule).ThenInclude(p => p.Module)
                                              .FirstOrDefaultAsync(x => x.NormalizedUserName == usuario);
```

Reemplazar por (se quita el `Include` de `UserSubmodules`, ya no se usa para calcular `rutas`):

```csharp
            var appUser = await _context.Users.AsTracking()
                                              .IgnoreQueryFilters()//SE AÑADIO ULTIMO
                                              .Include(p => p.Tenant)
                                              .FirstOrDefaultAsync(x => x.NormalizedUserName == usuario);
```

- [ ] **Step 4: Editar `ClaimsPrincipalExtension.cs`**

Reemplazar el contenido completo de `Backend/WEB_API/Authentication/ClaimsPrincipalExtension.cs`:

```csharp
using Newtonsoft.Json;
using Domain.DTO;
using Domain.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WEB_API.Authentication
{
    public static class ClaimsPrincipalExtension
    {
        public static AuthenticatedUser GetUser(this ClaimsPrincipal principal)
        {
            var userName = principal.FindFirstValue("username");

            var tenant = principal.FindFirstValue(ClaimConstants.TenantId);

            var nombre = principal.FindFirstValue("nombre");

            var profiles = principal.FindFirstValue("rutas");

            var empresa = principal.FindFirstValue("empresa");

            var rutaPorDefecto = principal.FindFirstValue("rutaPorDefecto");

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var accesosDetalles = JsonConvert.DeserializeObject<List<AccesosDetalle>>(profiles);

            var isSuperAdmin = principal.IsInRole("SuperAdmin");

            return new AuthenticatedUser(userName, tenant, accesosDetalles, nombre, empresa, isSuperAdmin, roles, rutaPorDefecto);
        }
    }


    public sealed class AuthenticatedUser
    {
        public string UserName { get; private set; }

        public string Tenant { get; private set; }

        public string Nombre { get; private set; }

        public string Empresa { get; private set; }

        public List<AccesosDetalle> Rutas { get; private set; }

        public bool IsSuperAdmin { get; private set; }

        public List<string> Roles { get; private set; }

        public string? RutaPorDefecto { get; private set; }

        public AuthenticatedUser(string username, string tenant, List<AccesosDetalle> rutas, string nombre, string empresa, bool isSuperAdmin, List<string> roles, string? rutaPorDefecto)
        {
            UserName = username;
            Tenant = tenant;
            Rutas = rutas;
            Nombre = nombre;
            Empresa = empresa;
            IsSuperAdmin = isSuperAdmin;
            Roles = roles;
            RutaPorDefecto = rutaPorDefecto;
        }
    }
}
```

- [ ] **Step 5: Compilar**

Run: `cd Backend && dotnet build --nologo -v quiet 2>&1 | grep -i error`
Expected: `0 Errores`. Si aparece un error de ciclo de dependencias o de resolución de `IRoleRepository` al levantar el servidor (no en compile-time), revisar que Task 4 Step 4 (registro en DI) ya se haya hecho — sin eso, `AuthenticationRepository` no puede resolver `IRoleRepository` en runtime.

- [ ] **Step 6: Probar manualmente el login**

Run: `cd Backend/WEB_API && dotnet run --urls http://localhost:5080`

Hacer login con un usuario de prueba que tenga al menos un rol asignado (si todavía no creaste ninguno via API, este paso se puede repetir después de Task 7, que crea el rol Admin automáticamente para tenants existentes). Confirmar en la respuesta de `POST /api/autenticacion/token` que el JWT decodificado (pegarlo en jwt.io o similar) tiene los claims `rutas` y, si el usuario tiene rol con `RutaPorDefecto`, también `rutaPorDefecto`.

- [ ] **Step 7: Commit**

```bash
cd Backend
git add Infrastructure/Repositories/AuthenticationRepository.cs WEB_API/Authentication/ClaimsPrincipalExtension.cs
git commit -m "feat: rutas y rutaPorDefecto en el JWT salen de los roles del usuario

Antes salian de UserSubmodules directo (ApplicationUserDto.Resumen).
Ahora usan RoleRepository.ResolverAccesoUsuario, que une los
submodulos de todos los roles del usuario y resuelve la ruta de
aterrizaje por prioridad. Se agrega el claim rutaPorDefecto y se
expone Roles/RutaPorDefecto en /api/autenticacion/@me."
```

---

## Task 6: TenantRepository asigna el rol Admin en vez de submódulos sueltos

**Files:**
- Modify: `Backend/Infrastructure/Repositories/TenantRepository.cs`

**Interfaces:**
- Consumes: nada nuevo de otras tasks (usa `RoleManager<Role>`, ya registrado en DI desde antes de este plan).
- Produces: el método `AsignarRolAdminAlUsuario` que reemplaza a `AsociarModuleUser` — Task 7 (migración de tenants existentes) sigue el mismo patrón pero no lo llama directamente (opera por fuera del request pipeline).

- [ ] **Step 1: Agregar `RoleManager<Role>` al constructor**

En `Backend/Infrastructure/Repositories/TenantRepository.cs`, buscar:

```csharp
public class TenantRepository : ITenantRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;
    private readonly UserManager<User> _userManager;



    public TenantRepository(SpaContext dbContext, IMapper mapper, UserManager<User> userManager)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        _userManager = userManager;
    }
```

Reemplazar por:

```csharp
public class TenantRepository : ITenantRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;



    public TenantRepository(SpaContext dbContext, IMapper mapper, UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        _userManager = userManager;
        _roleManager = roleManager;
    }
```

- [ ] **Step 2: Reemplazar `AsociarModuleUser` por `AsignarRolAdminAlUsuario`**

Buscar el método completo:

```csharp
    private async Task AsociarModuleUser(string userId, string Tenant)
    {
        string dateString = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss");
        DateTime FechaRegistro = DateTime.ParseExact(dateString, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        // Todos los submódulos existentes en el catálogo, no una lista fija: así el admin
        // de una empresa nueva arranca con acceso completo sin depender de mantener este código.
        var submoduleIds = await dbContext.AspNetSubModule.AsNoTracking().Select(s => s.Identificador).ToListAsync();

        var newUserSubModule = submoduleIds.Select(submoduleId => new AspNetUserSubModule
        {
            UserId = userId,
            SubmoduleId = submoduleId,
            UsuarioCreacion = "ADMIN",
            FechaCreacion = FechaRegistro,
            Estado = true,
            TenantId = Tenant
        }).ToList();

        await dbContext.AspNetUserSubModule.AddRangeAsync(newUserSubModule);
        await dbContext.SaveChangesRegularAsync();
    }
```

Reemplazar por:

```csharp
    private async Task<Role> ObtenerOCrearRolAdmin(string tenant)
    {
        var rolAdmin = await dbContext.Roles.IgnoreQueryFilters()
                                            .FirstOrDefaultAsync(r => r.TenantId == tenant && r.NormalizedName == "ADMIN");

        var submoduleIds = await dbContext.AspNetSubModule.AsNoTracking().Select(s => s.Identificador).ToListAsync();

        if (rolAdmin == null)
        {
            rolAdmin = new Role
            {
                Name = "Admin",
                TenantId = tenant,
                RutaPorDefecto = "/dashboard/productos",
                Prioridad = 0
            };

            var resultado = await _roleManager.CreateAsync(rolAdmin);

            if (!resultado.Succeeded)
                throw new Exception($"No se pudo crear el rol Admin para el tenant {tenant}: {string.Join("; ", resultado.Errors.Select(e => e.Description))}");
        }

        // Sincroniza: agrega los submodulos del catalogo que todavia no tenga (el catalogo
        // puede crecer con el tiempo), nunca quita los que ya tiene.
        var submodulosActuales = await dbContext.RoleSubmodule.IgnoreQueryFilters()
                                                               .Where(rs => rs.RoleId == rolAdmin.Id)
                                                               .Select(rs => rs.SubmoduleId)
                                                               .ToListAsync();

        var faltantes = submoduleIds.Except(submodulosActuales).ToList();

        foreach (var submoduleId in faltantes)
        {
            dbContext.RoleSubmodule.Add(new RoleSubmodule { RoleId = rolAdmin.Id, SubmoduleId = submoduleId, TenantId = tenant });
        }

        if (faltantes.Count > 0)
            await dbContext.SaveChangesRegularAsync();

        return rolAdmin;
    }

    private async Task AsignarRolAdminAlUsuario(string userId, string tenant)
    {
        var rolAdmin = await ObtenerOCrearRolAdmin(tenant);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception($"No se encontro el usuario {userId} recien creado para asignarle el rol Admin");

        await _userManager.AddToRoleAsync(user, rolAdmin.Name);
    }
```

- [ ] **Step 3: Actualizar los dos call sites**

Buscar (aparece dos veces, en `CreateEmpresa` y en el otro método de creación de tenant):

```csharp
            //Asociar Usuarios a los modulos
            await AsociarModuleUser(userResult.Id, userTenantName);
```

Reemplazar por:

```csharp
            //Asignar el rol Admin al usuario
            await AsignarRolAdminAlUsuario(userResult.Id, userTenantName);
```

Y la segunda aparición:

```csharp
            //Asociar Usuarios a los modulos
            await AsociarModuleUser(userResult.Id, newTenantName);
```

Reemplazar por:

```csharp
            //Asignar el rol Admin al usuario
            await AsignarRolAdminAlUsuario(userResult.Id, newTenantName);
```

(Confirmar con `grep -n "AsociarModuleUser" Infrastructure/Repositories/TenantRepository.cs` que no queda ninguna referencia después de este step — debe devolver 0 resultados.)

- [ ] **Step 4: Reescribir `ReasignarModulos`**

Buscar el método completo:

```csharp
    public async Task<(ServiceStatus, string)> ReasignarModulos(int identificador)
    {
        try
        {
            var tenant = await dbContext.Tenant.AsNoTracking().FirstOrDefaultAsync(t => t.Identificador == identificador);

            if (tenant is null)
                return (ServiceStatus.NotFound, "No existe el tenant");

            var usuarios = await dbContext.Users.Where(u => u.TenantId == identificador).ToListAsync();

            foreach (var usuario in usuarios)
            {
                var existentes = dbContext.AspNetUserSubModule.IgnoreQueryFilters().Where(x => x.UserId == usuario.Id);
                dbContext.AspNetUserSubModule.RemoveRange(existentes);
                await dbContext.SaveChangesRegularAsync();

                await AsociarModuleUser(usuario.Id, tenant.Name);
            }

            return (ServiceStatus.Ok, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al reasignar módulos -> {ex.InnerException?.Message ?? ex.Message}");
        }
```

(Fijarse que el cierre de esta función tiene una llave más abajo — solo reemplazar hasta el `catch` inclusive, dejando el cierre de método donde esté.)

Reemplazar por:

```csharp
    public async Task<(ServiceStatus, string)> ReasignarModulos(int identificador)
    {
        try
        {
            var tenant = await dbContext.Tenant.AsNoTracking().FirstOrDefaultAsync(t => t.Identificador == identificador);

            if (tenant is null)
                return (ServiceStatus.NotFound, "No existe el tenant");

            // Ya no hace falta tocar usuario por usuario: sincronizar el rol Admin del tenant
            // (agregar los submodulos nuevos del catalogo que todavia no tenga) alcanza, porque
            // los permisos de cada usuario salen de sus roles, no de asignaciones directas.
            await ObtenerOCrearRolAdmin(tenant.Name);

            return (ServiceStatus.Ok, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al reasignar módulos -> {ex.InnerException?.Message ?? ex.Message}");
        }
```

- [ ] **Step 5: Compilar**

Run: `cd Backend && dotnet build --nologo -v quiet 2>&1 | grep -i error`
Expected: `0 Errores`.

- [ ] **Step 6: Commit**

```bash
cd Backend
git add Infrastructure/Repositories/TenantRepository.cs
git commit -m "feat: crear tenant asigna el rol Admin, ya no submodulos sueltos

AsociarModuleUser -> AsignarRolAdminAlUsuario. ReasignarModulos ahora
sincroniza el rol Admin del tenant (agrega submodulos nuevos del
catalogo) en vez de recorrer usuario por usuario."
```

---

## Task 7: Migración de tenants existentes (backfill del rol Admin)

**Files:**
- Modify: `Backend/Infrastructure/Data/PuntoVentaDbContextData.cs`

**Interfaces:**
- Consumes: mismo patrón idempotente que `SeedCatalogoTenant` (Task 6 introdujo `ObtenerOCrearRolAdmin` en `TenantRepository`, pero este método corre en el pipeline de seeding al arrancar, sin `TenantRepository` disponible — se reimplementa la misma lógica directo contra `SpaContext`, igual que hace el resto de este archivo con `Seriecorrelativo`/`Metodopago`).
- Produces: nada que otra task consuma — es el paso final que dexa los tenants ya desplegados usables con el sistema de roles.

- [ ] **Step 1: Agregar la llamada al loop de tenants**

En `Backend/Infrastructure/Data/PuntoVentaDbContextData.cs`, buscar:

```csharp
                foreach (var tenantName in tenantNames)
                {
                    // Catálogos como TipoDocumento comparten los mismos Id numéricos entre
                    // tenants (el Id no es parte de una clave compuesta con TenantId). El
                    // change tracker de EF identifica entidades trackeadas solo por Id, así
                    // que sin limpiarlo, las entidades ya guardadas para un tenant chocan
                    // ("already being tracked") al intentar trackear el mismo Id para el
                    // siguiente tenant.
                    context.ChangeTracker.Clear();
                    await SeedCatalogoTenant(context, tenantName);
                }
```

Reemplazar por:

```csharp
                foreach (var tenantName in tenantNames)
                {
                    // Catálogos como TipoDocumento comparten los mismos Id numéricos entre
                    // tenants (el Id no es parte de una clave compuesta con TenantId). El
                    // change tracker de EF identifica entidades trackeadas solo por Id, así
                    // que sin limpiarlo, las entidades ya guardadas para un tenant chocan
                    // ("already being tracked") al intentar trackear el mismo Id para el
                    // siguiente tenant.
                    context.ChangeTracker.Clear();
                    await SeedCatalogoTenant(context, tenantName);
                    await MigrarRolAdmin(context, tenantName);
                }
```

- [ ] **Step 2: Agregar el método `MigrarRolAdmin`**

Agregar este método nuevo, después de `SeedCatalogoTenant` (buscar el cierre de ese método — el `}` que sigue a su último bloque, antes del siguiente método del archivo — y pegar `MigrarRolAdmin` justo después):

```csharp
        /// <summary>
        /// Backfill para tenants que ya existian antes del sistema de roles: crea (si falta)
        /// el rol "Admin" del tenant con todo el catalogo de submodulos, y se lo asigna a
        /// cualquier usuario que hoy tenga al menos un AspNetUserSubModule (antes del cambio a
        /// roles, solo el admin de cada tenant llegaba a tener alguno). Los usuarios sin ningun
        /// AspNetUserSubModule (empleados restringidos manejados hasta ahora por username
        /// hardcodeado en el frontend) quedan sin rol: el admin del tenant se los asigna a mano
        /// desde la pantalla de Roles.
        /// </summary>
        private static async Task MigrarRolAdmin(SpaContext context, string tenantId)
        {
            var rolAdmin = await context.Roles.IgnoreQueryFilters()
                                              .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.NormalizedName == "ADMIN");

            var submoduleIds = await context.AspNetSubModule.AsNoTracking().Select(s => s.Identificador).ToListAsync();

            if (rolAdmin == null)
            {
                rolAdmin = new Role
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    TenantId = tenantId,
                    RutaPorDefecto = "/dashboard/productos",
                    Prioridad = 0
                };

                await context.Roles.AddAsync(rolAdmin);
                await context.SaveChangesRegularAsync();
            }

            var submodulosActuales = await context.RoleSubmodule.IgnoreQueryFilters()
                                                                 .Where(rs => rs.RoleId == rolAdmin.Id)
                                                                 .Select(rs => rs.SubmoduleId)
                                                                 .ToListAsync();

            var faltantes = submoduleIds.Except(submodulosActuales).ToList();

            if (faltantes.Count > 0)
            {
                var nuevas = faltantes.Select(submoduleId => new RoleSubmodule
                {
                    RoleId = rolAdmin.Id,
                    SubmoduleId = submoduleId,
                    TenantId = tenantId,
                    UsuarioCreacion = "ADMIN",
                    FechaCreacion = DateTime.UtcNow.AddHours(-5),
                    Estado = true
                }).ToList();

                await context.RoleSubmodule.AddRangeAsync(nuevas);
                await context.SaveChangesRegularAsync();
            }

            var usuariosConAccesoDirecto = await context.AspNetUserSubModule.IgnoreQueryFilters()
                                                                            .Where(x => x.TenantId == tenantId)
                                                                            .Select(x => x.UserId)
                                                                            .Distinct()
                                                                            .ToListAsync();

            var yaTienenRolAdmin = await context.UserRoles.IgnoreQueryFilters()
                                                          .Where(ur => ur.RoleId == rolAdmin.Id)
                                                          .Select(ur => ur.UserId)
                                                          .ToListAsync();

            var porAsignar = usuariosConAccesoDirecto.Except(yaTienenRolAdmin).ToList();

            if (porAsignar.Count > 0)
            {
                var nuevasAsignaciones = porAsignar.Select(userId => new UserRol
                {
                    UserId = userId,
                    RoleId = rolAdmin.Id
                }).ToList();

                await context.UserRoles.AddRangeAsync(nuevasAsignaciones);
                await context.SaveChangesRegularAsync();
            }
        }
```

Verificar que `PuntoVentaDbContextData.cs` ya tenga `using Domain.Entities.Identity;` al principio del archivo (necesario para `Role`, `RoleSubmodule`, `UserRol`) — si no está, agregarlo junto a los demás `using` del principio del archivo.

Cambiar la firma de `private static async Task MigrarRolAdmin` a `internal static async Task MigrarRolAdmin` (mismo cuerpo, solo cambia el modificador de acceso) — hace falta para poder llamarlo directo desde el test del Step 4, sin pasar por todo `LoadDataAsync` (que además lee los JSON de catálogo desde disco, algo frágil de reproducir en el proyecto de test).

- [ ] **Step 3: Habilitar que el proyecto de tests vea los miembros `internal`**

En `Backend/Infrastructure/Infrastructure.csproj`, agregar un `ItemGroup` nuevo (por ejemplo, después del `ItemGroup` de `PackageReference`):

```xml
  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
      <_Parameter1>Infrastructure.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
```

- [ ] **Step 4: Test de la migración**

Crear `Backend/Tests/Infrastructure.Tests/MigrarRolAdminTests.cs`:

```csharp
using Domain.Entities.Identity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class MigrarRolAdminTests
{
    [Fact]
    public async Task MigrarRolAdmin_UsuarioConAccesoDirectoPrevio_QuedaConRolAdmin_OtroSinNingunoQuedaSinRol()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(tenantName: "TENANT_X"));
        using var _ = connection;

        context.AspNetModule.Add(new AspNetModule { Identificador = "200", Nombre = "Productos" });
        context.AspNetSubModule.Add(new AspNetSubModule { Identificador = "201", Nombre = "Productos", ModuloId = "200" });
        await context.SaveChangesAsync();

        // Usuario que ya tenia acceso directo (como el admin, antes de este cambio)
        context.AspNetUserSubModule.Add(new AspNetUserSubModule { UserId = "user-con-acceso", SubmoduleId = "201" });
        await context.SaveChangesAsync();

        await PuntoVentaDbContextData.MigrarRolAdmin(context, "TENANT_X");

        var rolAdmin = await context.Roles.IgnoreQueryFilters()
                                          .SingleAsync(r => r.TenantId == "TENANT_X" && r.NormalizedName == "ADMIN");

        Assert.Equal("/dashboard/productos", rolAdmin.RutaPorDefecto);
        Assert.Equal(0, rolAdmin.Prioridad);

        var submodulosDelRol = await context.RoleSubmodule.IgnoreQueryFilters()
                                                           .Where(rs => rs.RoleId == rolAdmin.Id)
                                                           .Select(rs => rs.SubmoduleId)
                                                           .ToListAsync();
        Assert.Contains("201", submodulosDelRol);

        var rolesDeUsuarioConAcceso = await context.UserRoles.IgnoreQueryFilters()
                                                              .Where(ur => ur.UserId == "user-con-acceso")
                                                              .Select(ur => ur.RoleId)
                                                              .ToListAsync();
        Assert.Contains(rolAdmin.Id, rolesDeUsuarioConAcceso);

        var rolesDeUsuarioSinAcceso = await context.UserRoles.IgnoreQueryFilters()
                                                              .Where(ur => ur.UserId == "user-sin-acceso")
                                                              .ToListAsync();
        Assert.Empty(rolesDeUsuarioSinAcceso);
    }

    [Fact]
    public async Task MigrarRolAdmin_CorridoDosVeces_EsIdempotente()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(tenantName: "TENANT_Y"));
        using var _ = connection;

        context.AspNetModule.Add(new AspNetModule { Identificador = "200", Nombre = "Productos" });
        context.AspNetSubModule.Add(new AspNetSubModule { Identificador = "201", Nombre = "Productos", ModuloId = "200" });
        await context.SaveChangesAsync();

        await PuntoVentaDbContextData.MigrarRolAdmin(context, "TENANT_Y");
        await PuntoVentaDbContextData.MigrarRolAdmin(context, "TENANT_Y");

        var cantidadRolesAdmin = await context.Roles.IgnoreQueryFilters()
                                                     .CountAsync(r => r.TenantId == "TENANT_Y" && r.NormalizedName == "ADMIN");

        Assert.Equal(1, cantidadRolesAdmin);
    }
}
```

- [ ] **Step 5: Correr los tests nuevos**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --nologo --filter "MigrarRolAdminTests" -v normal 2>&1 | tail -30`
Expected: los 2 tests en verde.

- [ ] **Step 6: Compilar**

Run: `cd Backend && dotnet build --nologo -v quiet 2>&1 | grep -i error`
Expected: `0 Errores`.

- [ ] **Step 7: Probar contra la base local**

Run: `cd Backend/WEB_API && dotnet run --urls http://localhost:5080` (esto dispara `LoadDataAsync` al arrancar, según el resto de este archivo).

Revisar los logs de arranque: no debe haber excepciones relacionadas a `MigrarRolAdmin`/`Role`/`RoleSubmodule`. Frenar el servidor.

Con un cliente de Postgres (o el mismo patrón que uses habitualmente para inspeccionar la base local), correr:

```sql
SELECT "Name", "TenantId", "RutaPorDefecto", "Prioridad" FROM "AspNetRoles" WHERE "NormalizedName" = 'ADMIN';
```

Expected: una fila por cada tenant que ya existía en la base, con `RutaPorDefecto = '/dashboard/productos'` y `Prioridad = 0`.

- [ ] **Step 8: Commit**

```bash
cd Backend
git add Infrastructure/Data/PuntoVentaDbContextData.cs Infrastructure/Infrastructure.csproj Tests/Infrastructure.Tests/MigrarRolAdminTests.cs
git commit -m "feat: backfill del rol Admin para tenants existentes

Se corre en cada arranque, idempotente, igual que el resto del
seeding de este archivo. Crea (si falta) el rol Admin de cada tenant
con el catalogo completo, y se lo asigna a los usuarios que ya tenian
acceso directo a submodulos (antes del cambio a roles, solo el admin
de cada tenant llegaba a tener alguno). MigrarRolAdmin pasa a internal
+ InternalsVisibleTo para poder testearlo sin pasar por LoadDataAsync
completo (que lee el catalogo desde JSON en disco)."
```

---

## Task 8: Catálogo — módulo/submódulo "Roles"

**Files:**
- Modify: `Backend/Infrastructure/Data/Default/module.json`
- Modify: `Backend/Infrastructure/Data/Default/subModule.json`

**Interfaces:**
- Produces: `AspNetModule.Identificador = "1400"`, `AspNetSubModule.Identificador = "1401"` — Task 15 (pantalla de Roles en el frontend) usa el `"1400"` como `code` en `menuSidebar`.

- [ ] **Step 1: Agregar el módulo**

En `Backend/Infrastructure/Data/Default/module.json`, agregar al final del arreglo (antes del `]` de cierre, con coma después del elemento anterior):

```json
  {
    "identificador": "1400",
    "nombre": "Roles",
    "usuarioCreacion": "ADMIN",
    "estado": "true"
  }
```

- [ ] **Step 2: Agregar el submódulo**

En `Backend/Infrastructure/Data/Default/subModule.json`, agregar al final del arreglo:

```json
  {
    "identificador": "1401",
    "nombre": "Roles",
    "moduloId": "1400",
    "usuarioCreacion": "ADMIN",
    "estado": "true"
  }
```

- [ ] **Step 3: Confirmar que el JSON sigue siendo válido**

Run: `cd Backend && python3 -c "import json; json.load(open('Infrastructure/Data/Default/module.json')); json.load(open('Infrastructure/Data/Default/subModule.json')); print('OK')"`

(Si no hay `python3` disponible, usar `node -e "JSON.parse(require('fs').readFileSync('Infrastructure/Data/Default/module.json')); JSON.parse(require('fs').readFileSync('Infrastructure/Data/Default/subModule.json')); console.log('OK')"` desde `Backend/`.)

Expected: `OK`, sin excepciones de parseo.

- [ ] **Step 4: Commit**

```bash
cd Backend
git add Infrastructure/Data/Default/module.json Infrastructure/Data/Default/subModule.json
git commit -m "feat: catalogo - modulo y submodulo Roles (1400/1401)"
```

---

## Task 9: Frontend — tipos `IMe` con roles/rutaPorDefecto

**Files:**
- Modify: `Frontend/src/redux/reducers/auth/interfaces/index.ts`

**Interfaces:**
- Produces: `IMe.roles` (`string[]`), `IMe.rutaPorDefecto` (`string | undefined`) — usados en Tasks 10, 12, 13, 14.

- [ ] **Step 1: Editar `IMe`**

En `Frontend/src/redux/reducers/auth/interfaces/index.ts`, buscar:

```typescript
export interface IMe {
    userName: string
    tenant: string
    rutas? : any
    nombre?:any
    empresa?:any;
    isSuperAdmin?: boolean;
}
```

Reemplazar por:

```typescript
export interface IMe {
    userName: string
    tenant: string
    rutas? : any
    nombre?:any
    empresa?:any;
    isSuperAdmin?: boolean;
    roles?: string[];
    rutaPorDefecto?: string;
}
```

- [ ] **Step 2: Typecheck**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | grep -i "auth/interfaces"`
Expected: sin salida (ningún error nuevo en ese archivo).

- [ ] **Step 3: Commit**

```bash
cd Frontend
git add src/redux/reducers/auth/interfaces/index.ts
git commit -m "feat: IMe expone roles y rutaPorDefecto"
```

---

## Task 10: Frontend — Login usa rutaPorDefecto en vez de la cadena de usernames

**Files:**
- Modify: `Frontend/src/presentation/views/Login/index.tsx`

**Interfaces:**
- Consumes: `me.rutaPorDefecto` (Task 9).
- Produces: redirige a `/sin-permisos` cuando no hay rol — Task 11 crea esa pantalla; hasta que exista, este task deja un 404 momentáneo si se prueba manualmente antes de hacer Task 11 (no es bloqueante para el build, sí para probar el flujo completo).

- [ ] **Step 1: Reemplazar el bloque de redirects**

En `Frontend/src/presentation/views/Login/index.tsx`, buscar TODO este bloque (desde `const checkTenantMatch` hasta el `window.location.href = "/dashboard/productos";` final, inclusive el cierre `}, [login, me, tenants]);`):

```typescript
    const checkTenantMatch = (username: string | undefined) => {
      if (!username) return false;
      const uLower = username.toLowerCase();
      if (Array.isArray(tenants)) {
        return tenants.some((t: any) => {
          if (!t) return false;
          if (typeof t === 'string') {
            return uLower.includes(t.toLowerCase());
          }
          if (typeof t === 'object') {
            const name = t.tenantNombre || t.nombre || t.id;
            return name && uLower.includes(String(name).toLowerCase());
          }
          return false;
        });
      }
      return false;
    };

    if (
      (token && me?.userName === "VENTAS2") ||
      (token && me?.userName === "VENTAS1") ||
      (token && me?.userName === "JRAMIREZ") ||
      (token && me?.userName === "DISCOBAR") ||
      (token && me?.userName === "SPASOLIS") 
    ) {
      window.location.href = "/facturacion";
      return;
    }
    if (
      /*  (token && me?.userName === "ADMIN") || */
      (token && me?.userName === "RECEPCION") || 
      (token && me?.nombre?.toLowerCase() === login?.username?.toLowerCase())
    ) {
      window.location.href = "/dashboard/productos";
      return;
    }
    console.log(login);
    if (login === null) {
      return;
    }
    const usernameLower = login?.username?.toLowerCase() || "";
    const meUsernameLower = me?.userName?.toLowerCase() || "";

    if (
      usernameLower.startsWith("ventas") ||
      meUsernameLower.startsWith("ventas") ||
      usernameLower.startsWith("barra1") ||
      meUsernameLower.startsWith("barra1")
    ) {
      window.location.href = "/facturacion";
      return;
    }
    if (
      usernameLower.startsWith("admin") ||
      checkTenantMatch(login?.username) ||
      usernameLower.startsWith("recepcion") ||
      usernameLower.startsWith("developer") ||
      usernameLower.startsWith("jramirez") ||
      usernameLower.startsWith("discobar") ||
      usernameLower.startsWith("restaurant") ||
      usernameLower.startsWith("spasolis") ||
      meUsernameLower.startsWith("recepcion") ||
      checkTenantMatch(me?.userName) ||
      meUsernameLower.startsWith("contadora")
    ) {
      window.location.href = "/dashboard/productos";
      return;
    }
    if (usernameLower.startsWith("contadora")) {
      window.location.href = "/dashboard/documentos-facturados";
      return;
    }

    // Cualquier usuario autenticado que no matchea ninguna regla legacy de arriba
    // (ej. el admin de un tenant nuevo) cae aquí en vez de quedar pegado en el login.
    window.location.href = "/dashboard/productos";
  }, [login, me, tenants]);
```

Reemplazar por:

```typescript
    if (!token || !me) return;

    if (me?.rutaPorDefecto) {
      window.location.href = me.rutaPorDefecto;
    } else {
      window.location.href = "/sin-permisos";
    }
  }, [login, me, tenants]);
```

- [ ] **Step 2: Confirmar que `tenants`/`checkTenantMatch` no quedan variables sin usar**

Run: `cd Frontend && grep -n "checkTenantMatch\|tenants" src/presentation/views/Login/index.tsx`

Si `tenants` sigue usándose en otro lado del archivo (ej. en el JSX para mostrar un selector), dejar el `useSelector` que lo trae. Si `checkTenantMatch` no aparece en ningún otro lado, ya se borró entero en el Step 1 — no queda nada suelto que limpiar.

- [ ] **Step 3: Typecheck**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | grep -i "Login/index"`
Expected: sin salida.

- [ ] **Step 4: Commit**

```bash
cd Frontend
git add src/presentation/views/Login/index.tsx
git commit -m "feat: login aterriza segun rutaPorDefecto del rol, no username hardcodeado

Borra la cascada de ifs por username especifico (VENTAS1/VENTAS2/
JRAMIREZ/DISCOBAR/SPASOLIS/RECEPCION/CONTADORA/admin/developer/
restaurant/barra1) y el match por nombre de tenant. Si el usuario no
tiene ningun rol asignado, aterriza en /sin-permisos."
```

---

## Task 11: Frontend — pantalla `/sin-permisos`

**Files:**
- Create: `Frontend/src/presentation/views/SinPermisos/index.tsx`
- Create: `Frontend/src/presentation/views/SinPermisos/sinPermisos.module.css`
- Modify: `Frontend/src/infraestructure/Dashboard.tsx`

**Interfaces:**
- Consumes: `state.auth.me.empresa` (ya existe en `IMe`, ver Task 9).
- Produces: ruta `/sin-permisos` — Task 10 ya redirige ahí.

- [ ] **Step 1: Crear el componente**

```tsx
import { useAppSelector } from "../../../redux/store";
import { RootState } from "../../../redux/rootState";
import { IAuthState } from "../../../redux/reducers/auth/interfaces";
import styles from "./sinPermisos.module.css";

const SinPermisos = () => {
  const { me }: IAuthState = useAppSelector((state: RootState) => state.auth);

  const logout = () => {
    localStorage.clear();
    window.location.href = "/";
  };

  return (
    <div className={styles.wrapper}>
      <div className={styles.card}>
        <h1>Bienvenido a {me?.empresa || "tu empresa"}</h1>
        <p>
          Todavía no tenés ningún permiso asignado. Contactá al administrador
          de tu empresa para que te asigne un rol.
        </p>
        <button onClick={logout}>Cerrar sesión</button>
      </div>
    </div>
  );
};

export default SinPermisos;
```

- [ ] **Step 2: Crear el CSS**

```css
.wrapper {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--neutral-100, #f5f6f8);
  padding: 24px;
}

.card {
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 8px 30px -12px rgba(0, 0, 0, 0.15);
  padding: 40px 32px;
  max-width: 420px;
  width: 100%;
  text-align: center;
}

.card h1 {
  font-size: 20px;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 12px;
}

.card p {
  font-size: 14px;
  color: #64748b;
  margin: 0 0 24px;
  line-height: 1.5;
}

.card button {
  background: var(--brand-500, #2997fe);
  color: #fff;
  border: none;
  border-radius: 8px;
  padding: 10px 20px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}
```

- [ ] **Step 3: Registrar la ruta**

En `Frontend/src/infraestructure/Dashboard.tsx`, buscar:

```typescript
import Login from "../presentation/views/Login";
```

Agregar después:

```typescript
import SinPermisos from "../presentation/views/SinPermisos";
```

Buscar:

```tsx
          <Route path="/" element={<Login />} />
          <Route path="/facturacion" element={<Facturacion />} />
```

Agregar una línea entre esas dos:

```tsx
          <Route path="/" element={<Login />} />
          <Route path="/sin-permisos" element={<SinPermisos />} />
          <Route path="/facturacion" element={<Facturacion />} />
```

- [ ] **Step 4: Typecheck**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | grep -i "SinPermisos\|Dashboard.tsx"`
Expected: sin salida.

- [ ] **Step 5: Probar visualmente**

Run: `cd Frontend && npm run dev` (o el comando que ya uses para levantar el dev server).

Navegar directo a `http://localhost:<puerto>/sin-permisos` en el navegador. Confirmar que se ve la tarjeta centrada con el mensaje (el nombre de empresa puede salir vacío/"tu empresa" si no hay sesión iniciada — es esperado navegando directo a la ruta sin login). Frenar el dev server.

- [ ] **Step 6: Commit**

```bash
cd Frontend
git add src/presentation/views/SinPermisos/ src/infraestructure/Dashboard.tsx
git commit -m "feat: pantalla /sin-permisos para usuarios sin rol asignado"
```

---

## Task 12: Frontend — Navbar oculta "Ir a ventas" por rol, no por username

**Files:**
- Modify: `Frontend/src/components/Sidebar/Navbar/index.tsx`

- [ ] **Step 1: Reemplazar el check**

Buscar:

```tsx
              {
                !me?.userName?.startsWith('RECEPCION') && !me?.userName?.startsWith('CONTADORA') &&
                <div className={styles['btn-go-sales']}>
```

Reemplazar por:

```tsx
              {
                !me?.roles?.includes('Recepcion') && !me?.roles?.includes('Contadora') &&
                <div className={styles['btn-go-sales']}>
```

- [ ] **Step 2: Typecheck**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | grep -i "Navbar/index"`
Expected: sin salida.

- [ ] **Step 3: Commit**

```bash
cd Frontend
git add src/components/Sidebar/Navbar/index.tsx
git commit -m "fix: ocultar boton Ir a ventas por rol, no por username hardcodeado"
```

---

## Task 13: Frontend — Facturacion redirige por rol, no por username

**Files:**
- Modify: `Frontend/src/presentation/views/Modules/Facturacion/index.tsx`

- [ ] **Step 1: Reemplazar el check**

Buscar:

```tsx
    useEffect(() => {
        if (me?.userName.startsWith('RECEPCION') || me?.userName.startsWith('CONTADORA')) {
            window.location.href = '/dashboard/productos';
        }
    }, [me])
```

Reemplazar por:

```tsx
    useEffect(() => {
        if (me?.roles?.includes('Recepcion') || me?.roles?.includes('Contadora')) {
            window.location.href = '/dashboard/productos';
        }
    }, [me])
```

- [ ] **Step 2: Typecheck**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | grep -i "Facturacion/index"`
Expected: sin salida.

- [ ] **Step 3: Commit**

```bash
cd Frontend
git add src/presentation/views/Modules/Facturacion/index.tsx
git commit -m "fix: redirigir fuera de facturacion por rol, no por username hardcodeado"
```

---

## Task 14: Frontend — restricción de categoría de VENTAS1/VENTAS2 por rol

**Files:**
- Modify: `Frontend/src/presentation/views/Modules/Facturacion/ProductosFiltradosByCard/index.tsx`

- [ ] **Step 1: Reemplazar el primer check (línea 57)**

Buscar:

```tsx
    const productosVentas = me?.userName === 'VENTAS1' || me?.userName === 'VENTAS2' ? productsFilters?.filter((item: any) => item?.categoria?.id === 3 && item?.grupoId === 17) : productsFilters
```

Reemplazar por:

```tsx
    const productosVentas = me?.roles?.includes('Ventas') ? productsFilters?.filter((item: any) => item?.categoria?.id === 3 && item?.grupoId === 17) : productsFilters
```

- [ ] **Step 2: Reemplazar el segundo check (línea 64)**

Buscar:

```tsx
                    {
                        me?.userName === 'VENTAS1' || me?.userName === 'VENTAS2' ? '' : (
```

Reemplazar por:

```tsx
                    {
                        me?.roles?.includes('Ventas') ? '' : (
```

- [ ] **Step 3: Typecheck**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | grep -i "ProductosFiltradosByCard"`
Expected: sin salida.

- [ ] **Step 4: Commit**

```bash
cd Frontend
git add src/presentation/views/Modules/Facturacion/ProductosFiltradosByCard/index.tsx
git commit -m "fix: restriccion de categoria para el rol Ventas, no por username"
```

---

## Task 15: Frontend — pantalla "Roles" (admin)

**Files:**
- Create: `Frontend/src/presentation/views/Modules/Admin/Views/Roles/index.tsx`
- Modify: `Frontend/src/infraestructure/Dashboard.tsx`
- Modify: `Frontend/src/infraestructure/MData/MData.ts`

**Interfaces:**
- Consumes: `GET/POST/PUT/DELETE /api/roles`, `GET /api/roles/catalogo-submodulos` (Task 4).

- [ ] **Step 1: Crear la pantalla**

Mirroring el patrón exacto de `Frontend/src/presentation/views/Modules/Admin/Views/Usuarios/index.tsx` (tabla + modal de crear, sin edición inline — acá se agrega edición porque hace falta tocar los submódulos después de crear el rol):

```tsx
import { useEffect, useState } from "react";
import Modal from "react-modal";
import { toast } from "sonner";
import axiosInstance from "../../../../../../utils/axios";
import Input from "../../../../../../components/Input";
import { Button } from "@tremor/react";
import { TableSkeleton } from "../../../../../../components/Skeleton";

Modal.setAppElement("#root");

const modalStyle = {
  overlay: {
    backgroundColor: "rgba(0,0,0,0.5)",
    zIndex: 999,
  },
  content: {
    top: "50%",
    left: "50%",
    right: "auto",
    bottom: "auto",
    marginRight: "-50%",
    transform: "translate(-50%, -50%)",
    width: "min(92vw, 560px)",
    maxHeight: "85vh",
    overflowY: "auto",
    borderRadius: "1rem",
    padding: "0",
    border: "none",
  },
} as any;

const RUTAS_VALIDAS = [
  { value: "/facturacion", label: "Facturacion (ventas)" },
  { value: "/dashboard/productos", label: "Dashboard - Productos" },
  { value: "/dashboard/documentos-facturados", label: "Dashboard - Documentos facturados" },
  { value: "/dashboard/reporte-cierre-caja", label: "Dashboard - Reporte cierre de caja" },
  { value: "/dashboard/ventas-realizadas", label: "Dashboard - Ventas realizadas" },
];

interface IRole {
  id: string;
  nombre: string;
  rutaPorDefecto: string | null;
  prioridad: number;
  cantidadUsuarios: number;
  submoduleIds: string[];
}

interface ICatalogoSubmodulo {
  modulo: string;
  moduloNombre: string;
  subModulos: { subModulo: string; subModuloNombre: string }[];
}

const initialForm = {
  nombre: "",
  rutaPorDefecto: RUTAS_VALIDAS[0].value,
  prioridad: 100,
  submoduleIds: [] as string[],
};

export const Roles = () => {
  const [roles, setRoles] = useState<IRole[]>([]);
  const [catalogo, setCatalogo] = useState<ICatalogoSubmodulo[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [form, setForm] = useState(initialForm);

  const loadRoles = async () => {
    setLoading(true);
    try {
      const { data }: any = await axiosInstance.get(`/roles`);
      setRoles(data?.data ?? []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message ?? "Error al listar roles");
    } finally {
      setLoading(false);
    }
  };

  const loadCatalogo = async () => {
    try {
      const { data }: any = await axiosInstance.get(`/roles/catalogo-submodulos`);
      setCatalogo(data?.data ?? []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message ?? "Error al cargar el catalogo de pantallas");
    }
  };

  useEffect(() => {
    loadRoles();
    loadCatalogo();
  }, []);

  const abrirNuevo = () => {
    setEditandoId(null);
    setForm(initialForm);
    setModalOpen(true);
  };

  const abrirEditar = (rol: IRole) => {
    setEditandoId(rol.id);
    setForm({
      nombre: rol.nombre,
      rutaPorDefecto: rol.rutaPorDefecto || RUTAS_VALIDAS[0].value,
      prioridad: rol.prioridad,
      submoduleIds: rol.submoduleIds,
    });
    setModalOpen(true);
  };

  const handleChange = (e: any) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const toggleSubmodulo = (submoduloId: string) => {
    setForm((prev) => ({
      ...prev,
      submoduleIds: prev.submoduleIds.includes(submoduloId)
        ? prev.submoduleIds.filter((id) => id !== submoduloId)
        : [...prev.submoduleIds, submoduloId],
    }));
  };

  const handleSubmit = async () => {
    if (!form.nombre.trim()) return toast.error("El nombre del rol es obligatorio");

    setGuardando(true);
    try {
      if (editandoId) {
        await axiosInstance.put(`/roles/${editandoId}`, form);
        toast.success("Rol actualizado exitosamente");
      } else {
        await axiosInstance.post(`/roles`, form);
        toast.success("Rol creado exitosamente");
      }
      setModalOpen(false);
      setForm(initialForm);
      loadRoles();
    } catch (error: any) {
      toast.error(error?.response?.data?.message ?? "Error al guardar el rol");
    } finally {
      setGuardando(false);
    }
  };

  const eliminarRol = async (rol: IRole) => {
    try {
      await axiosInstance.delete(`/roles/${rol.id}`);
      toast.success("Rol eliminado");
      loadRoles();
    } catch (error: any) {
      toast.error(error?.response?.data?.message ?? "Error al eliminar el rol");
    }
  };

  return (
    <div className="w-full">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">Roles</h3>
          <p className="text-sm text-gray-500">
            Define que pantallas ve cada rol y a donde aterriza tras iniciar sesion.
          </p>
        </div>
        <Button size="sm" onClick={abrirNuevo}>
          Agregar rol
        </Button>
      </div>

      <div className="relative overflow-x-auto sm:rounded-lg border border-gray-200">
        <table className="w-full text-sm text-left text-gray-500">
          <thead className="text-xs text-gray-700 uppercase bg-gray-50">
            <tr>
              <th scope="col" className="px-4 py-3">Nombre</th>
              <th scope="col" className="px-4 py-3">Ruta de aterrizaje</th>
              <th scope="col" className="px-4 py-3">Prioridad</th>
              <th scope="col" className="px-4 py-3">Usuarios</th>
              <th scope="col" className="px-4 py-3">Acciones</th>
            </tr>
          </thead>
          {loading ? (
            <tbody>
              <tr><td colSpan={5} style={{ padding: 0 }}>
                <TableSkeleton columns={5} />
              </td></tr>
            </tbody>
          ) : (
          <tbody>
            {!loading && roles.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center">No hay roles creados todavia</td>
              </tr>
            )}
            {!loading &&
              roles.map((r) => (
                <tr key={r.id} className="bg-white border-b hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900">{r.nombre}</td>
                  <td className="px-4 py-3">{r.rutaPorDefecto || "-"}</td>
                  <td className="px-4 py-3">{r.prioridad}</td>
                  <td className="px-4 py-3">{r.cantidadUsuarios}</td>
                  <td className="px-4 py-3 flex gap-3">
                    <button className="text-blue-600 hover:underline" onClick={() => abrirEditar(r)}>Editar</button>
                    <button
                      className="text-red-600 hover:underline disabled:opacity-40 disabled:cursor-not-allowed"
                      disabled={r.cantidadUsuarios > 0}
                      title={r.cantidadUsuarios > 0 ? "Tiene usuarios asignados" : ""}
                      onClick={() => eliminarRol(r)}
                    >
                      Eliminar
                    </button>
                  </td>
                </tr>
              ))}
          </tbody>
          )}
        </table>
      </div>

      <Modal
        isOpen={modalOpen}
        onRequestClose={() => setModalOpen(false)}
        style={modalStyle}
      >
        <div className="p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            {editandoId ? "Editar rol" : "Nuevo rol"}
          </h3>
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2">
              <Input isLabel label="Nombre *" name="nombre" value={form.nombre} onChange={handleChange} />
            </div>
            <div>
              <label className="text-sm text-gray-700 block mb-1">Ruta de aterrizaje</label>
              <select
                name="rutaPorDefecto"
                value={form.rutaPorDefecto}
                onChange={handleChange}
                className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
              >
                {RUTAS_VALIDAS.map((r) => (
                  <option key={r.value} value={r.value}>{r.label}</option>
                ))}
              </select>
            </div>
            <div>
              <Input isLabel label="Prioridad" name="prioridad" type="number" value={form.prioridad} onChange={handleChange} />
            </div>
          </div>

          <div className="mt-4">
            <label className="text-sm text-gray-700 block mb-2">Pantallas que puede ver</label>
            <div className="max-h-64 overflow-y-auto border border-gray-200 rounded-md p-3 space-y-3">
              {catalogo.map((mod) => (
                <div key={mod.modulo}>
                  <p className="text-xs font-semibold text-gray-500 uppercase mb-1">{mod.moduloNombre}</p>
                  {mod.subModulos.map((sub) => (
                    <label key={sub.subModulo} className="flex items-center gap-2 text-sm text-gray-700 mb-1">
                      <input
                        type="checkbox"
                        checked={form.submoduleIds.includes(sub.subModulo)}
                        onChange={() => toggleSubmodulo(sub.subModulo)}
                      />
                      {sub.subModuloNombre}
                    </label>
                  ))}
                </div>
              ))}
            </div>
          </div>

          <div className="flex justify-end gap-3 mt-6">
            <Button size="sm" variant="secondary" onClick={() => setModalOpen(false)}>
              Cancelar
            </Button>
            <Button size="sm" onClick={handleSubmit} disabled={guardando}>
              {guardando ? "Guardando..." : editandoId ? "Guardar cambios" : "Crear rol"}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
```

- [ ] **Step 2: Registrar la ruta**

En `Frontend/src/infraestructure/Dashboard.tsx`, buscar:

```typescript
import { Empresas } from "../presentation/views/Modules/Admin/Views/Empresas";
```

Agregar después:

```typescript
import { Roles } from "../presentation/views/Modules/Admin/Views/Roles";
```

Buscar (dentro de las rutas anidadas bajo `/dashboard`):

```tsx
            <Route path="empresas" element={<Empresas />}/>
```

Agregar después:

```tsx
            <Route path="roles" element={<Roles />}/>
```

- [ ] **Step 3: Agregar la entrada de menú**

En `Frontend/src/infraestructure/MData/MData.ts`, buscar el último elemento del arreglo `menuSidebar` (el que tiene `code: "1300"`, "Gastos") y agregar un elemento nuevo justo después de ese bloque (antes del `]` de cierre del arreglo, respetando la coma):

```typescript
  {
    code: "1400",
    id: 14,
    value: "Roles",
    icon: "mdi:account-key-outline",
    url: "dashboard/roles",
  },
```

- [ ] **Step 4: Typecheck**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | tail -30`
Expected: sin errores nuevos relacionados a `Roles`, `Dashboard.tsx` o `MData.ts`.

- [ ] **Step 5: Probar visualmente**

Run: `cd Frontend && npm run dev`, con el backend (Task 1-8) corriendo en paralelo y logueado con un usuario que tenga el rol Admin (creado por la migración de Task 7, o creado a mano).

Navegar a `/dashboard/roles`. Confirmar: la tabla carga (vacía o con el rol Admin si ya existía), el botón "Agregar rol" abre el modal con el árbol de checkboxes poblado desde el catálogo, crear un rol de prueba funciona y aparece en la tabla, editarlo funciona, e intentar eliminar un rol con usuarios asignados muestra el botón deshabilitado. Frenar el dev server.

- [ ] **Step 6: Commit**

```bash
cd Frontend
git add src/presentation/views/Modules/Admin/Views/Roles/ src/infraestructure/Dashboard.tsx src/infraestructure/MData/MData.ts
git commit -m "feat: pantalla de administracion de roles"
```

---

## Task 16: Frontend — asignar roles a un usuario

**Files:**
- Modify: `Frontend/src/presentation/views/Modules/Admin/Views/Usuarios/index.tsx`

**Interfaces:**
- Consumes: `GET /api/roles` (Task 4/15), `PUT /api/roles/asignar-usuario` (Task 4).

- [ ] **Step 1: Traer la lista de roles y agregar estado de asignación**

En `Frontend/src/presentation/views/Modules/Admin/Views/Usuarios/index.tsx`, buscar:

```tsx
export const Usuarios = () => {
  const [usuarios, setUsuarios] = useState<IUsuario[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [creando, setCreando] = useState(false);
  const [form, setForm] = useState(initialForm);
```

Reemplazar por:

```tsx
interface IRoleOption {
  id: string;
  nombre: string;
}

export const Usuarios = () => {
  const [usuarios, setUsuarios] = useState<IUsuario[]>([]);
  const [roles, setRoles] = useState<IRoleOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [creando, setCreando] = useState(false);
  const [form, setForm] = useState(initialForm);
  const [modalRolesOpen, setModalRolesOpen] = useState(false);
  const [usuarioRoles, setUsuarioRoles] = useState<IUsuario | null>(null);
  const [rolesSeleccionados, setRolesSeleccionados] = useState<string[]>([]);
  const [guardandoRoles, setGuardandoRoles] = useState(false);
```

- [ ] **Step 2: Cargar roles y mostrar la asignación actual por usuario**

`ListarUsuarios` (backend, `UsersRepository.cs`) hoy no devuelve los roles de cada usuario — antes de este paso agregar eso en el backend. Volver a `Backend/Infrastructure/Repositories/UsersRepository.cs`, buscar dentro de `ListarUsuarios`:

```csharp
                var usuarios = await _context.Users.AsNoTracking()
                                                   .Where(u => appUser != null && u.TenantId == appUser.TenantId)
                                                   .OrderBy(u => u.UserName)
                                                   .Select(q => new
                                                   {
                                                       id = q.Id,
                                                       usuario = q.UserName,
                                                       nombres = q.FirstName,
                                                       apellidos = q.LastName,
                                                       email = q.Email ?? "",
                                                       telefono = q.PhoneNumber ?? "",
                                                       estado = q.Estado,
                                                       fechaCreacion = q.FechaCreacion
                                                   }).ToListAsync();
```

Reemplazar por:

```csharp
                var usuarios = await _context.Users.AsNoTracking()
                                                   .Where(u => appUser != null && u.TenantId == appUser.TenantId)
                                                   .OrderBy(u => u.UserName)
                                                   .Select(q => new
                                                   {
                                                       id = q.Id,
                                                       usuario = q.UserName,
                                                       nombres = q.FirstName,
                                                       apellidos = q.LastName,
                                                       email = q.Email ?? "",
                                                       telefono = q.PhoneNumber ?? "",
                                                       estado = q.Estado,
                                                       fechaCreacion = q.FechaCreacion,
                                                       roleIds = _context.UserRoles.Where(ur => ur.UserId == q.Id).Select(ur => ur.RoleId).ToList()
                                                   }).ToListAsync();
```

Run: `cd Backend && dotnet build --nologo -v quiet 2>&1 | grep -i error` — expected `0 Errores`. Commit este cambio de backend junto con el resto de este task al final del Step 6.

- [ ] **Step 3: Frontend — cargar roles y abrir el modal de asignación**

En el mismo archivo de Usuarios, actualizar `IUsuario` (buscar la interfaz `IUsuario` cerca del principio del archivo):

```typescript
interface IUsuario {
  id: string;
  usuario: string;
  nombres: string;
  apellidos: string;
  email: string;
  telefono: string;
  estado: boolean;
  fechaCreacion: string;
}
```

Reemplazar por:

```typescript
interface IUsuario {
  id: string;
  usuario: string;
  nombres: string;
  apellidos: string;
  email: string;
  telefono: string;
  estado: boolean;
  fechaCreacion: string;
  roleIds: string[];
}
```

Buscar:

```tsx
  useEffect(() => {
    loadUsers();
  }, []);
```

Reemplazar por:

```tsx
  const loadRoles = async () => {
    try {
      const { data }: any = await axiosInstance.get(`/roles`);
      setRoles((data?.data ?? []).map((r: any) => ({ id: r.id, nombre: r.nombre })));
    } catch (error: any) {
      toast.error(error?.response?.data?.message ?? "Error al listar roles");
    }
  };

  useEffect(() => {
    loadUsers();
    loadRoles();
  }, []);

  const abrirAsignarRoles = (usuario: IUsuario) => {
    setUsuarioRoles(usuario);
    setRolesSeleccionados(usuario.roleIds);
    setModalRolesOpen(true);
  };

  const toggleRol = (roleId: string) => {
    setRolesSeleccionados((prev) =>
      prev.includes(roleId) ? prev.filter((id) => id !== roleId) : [...prev, roleId]
    );
  };

  const guardarRoles = async () => {
    if (!usuarioRoles) return;
    setGuardandoRoles(true);
    try {
      await axiosInstance.put(`/roles/asignar-usuario`, {
        userId: usuarioRoles.id,
        roleIds: rolesSeleccionados,
      });
      toast.success("Roles asignados correctamente");
      setModalRolesOpen(false);
      loadUsers();
    } catch (error: any) {
      toast.error(error?.response?.data?.message ?? "Error al asignar roles");
    } finally {
      setGuardandoRoles(false);
    }
  };
```

- [ ] **Step 4: Agregar la columna y el botón en la tabla**

Buscar:

```tsx
              <th scope="col" className="px-4 py-3">Estado</th>
            </tr>
          </thead>
```

Reemplazar por:

```tsx
              <th scope="col" className="px-4 py-3">Estado</th>
              <th scope="col" className="px-4 py-3">Roles</th>
            </tr>
          </thead>
```

Buscar:

```tsx
                  <td className="px-4 py-3">
                    <span className={u.estado ? "text-green-600" : "text-red-600"}>
                      {u.estado ? "Activo" : "Inactivo"}
                    </span>
                  </td>
                </tr>
              ))}
```

Reemplazar por:

```tsx
                  <td className="px-4 py-3">
                    <span className={u.estado ? "text-green-600" : "text-red-600"}>
                      {u.estado ? "Activo" : "Inactivo"}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <button className="text-blue-600 hover:underline" onClick={() => abrirAsignarRoles(u)}>
                      {u.roleIds.length > 0 ? `${u.roleIds.length} asignado(s)` : "Asignar"}
                    </button>
                  </td>
                </tr>
              ))}
```

También actualizar el `colSpan` del `TableSkeleton` y de la fila "No hay usuarios registrados" de `7` a `8` (una columna más):

Buscar (dos apariciones):
```tsx
                <TableSkeleton columns={7} />
```
```tsx
                <td colSpan={7} className="px-4 py-6 text-center">No hay usuarios registrados</td>
```

Reemplazar por:
```tsx
                <TableSkeleton columns={8} />
```
```tsx
                <td colSpan={8} className="px-4 py-6 text-center">No hay usuarios registrados</td>
```

- [ ] **Step 5: Agregar el modal de asignación de roles**

Buscar el cierre del componente, justo antes de:

```tsx
    </div>
  );
};
```

(el que cierra el `<div className="w-full">` raíz, después del `</Modal>` de creación de usuario) — agregar un segundo `<Modal>` ahí, antes de ese cierre:

```tsx
      <Modal
        isOpen={modalRolesOpen}
        onRequestClose={() => setModalRolesOpen(false)}
        style={modalStyle}
      >
        <div className="p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Roles de {usuarioRoles?.usuario}
          </h3>
          <div className="max-h-64 overflow-y-auto border border-gray-200 rounded-md p-3 space-y-2">
            {roles.length === 0 && (
              <p className="text-sm text-gray-500">No hay roles creados todavia.</p>
            )}
            {roles.map((r) => (
              <label key={r.id} className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={rolesSeleccionados.includes(r.id)}
                  onChange={() => toggleRol(r.id)}
                />
                {r.nombre}
              </label>
            ))}
          </div>
          <div className="flex justify-end gap-3 mt-6">
            <Button size="sm" variant="secondary" onClick={() => setModalRolesOpen(false)}>
              Cancelar
            </Button>
            <Button size="sm" onClick={guardarRoles} disabled={guardandoRoles}>
              {guardandoRoles ? "Guardando..." : "Guardar"}
            </Button>
          </div>
        </div>
      </Modal>
```

- [ ] **Step 6: Typecheck, probar y commit**

Run: `cd Frontend && npx tsc --noEmit -p tsconfig.json 2>&1 | grep -i "Usuarios/index"`
Expected: sin salida.

Probar manualmente (`npm run dev` + backend corriendo): en `/dashboard/usuarios`, click en "Asignar" para un usuario, marcar el rol "Admin" (u otro creado en Task 15), guardar, confirmar que la columna Roles pasa a mostrar "1 asignado(s)". Confirmar en la base o volviendo a hacer login con ese usuario que el JWT ahora trae `rutas`/`rutaPorDefecto` acorde al rol asignado.

```bash
cd Backend
git add Infrastructure/Repositories/UsersRepository.cs
git commit -m "feat: ListarUsuarios expone los roleIds de cada usuario"

cd ../Frontend
git add src/presentation/views/Modules/Admin/Views/Usuarios/index.tsx
git commit -m "feat: asignar roles a un usuario desde la pantalla de Usuarios"
```

---

## Verificación final end-to-end

- [ ] Con el backend y frontend locales corriendo: crear un tenant nuevo (o usar uno existente ya migrado por Task 7), confirmar que su admin tiene el rol "Admin" y aterriza en `/dashboard/productos`.
- [ ] Crear un rol "Ventas" con `RutaPorDefecto=/facturacion`, submódulo de Productos únicamente, asignárselo a un usuario de prueba, loguearse con ese usuario: debe aterrizar en `/facturacion` y el Sidebar debe mostrar solo el módulo de Productos.
- [ ] Crear un usuario sin ningún rol, loguearse: debe aterrizar en `/sin-permisos` con el nombre de la empresa.
- [ ] Correr toda la suite de tests: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --nologo` — todos en verde.
- [ ] `cd Backend/WEB_API && dotnet publish ./Spa.Api.csproj -c release -o /tmp/publish_check --nologo` — 0 errores (mismo comando que corre el build de Docker).
- [ ] `cd Frontend && npx tsc --noEmit -p tsconfig.json` — 0 errores.
