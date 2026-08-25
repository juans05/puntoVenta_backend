# ROI de publicidad de Facebook por producto — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "ROI Publicidad" screen to the existing PuntoVenta POS where the user uploads a Meta Ads Manager Excel export, maps each ad to a product, and sees real ROI (revenue from actual sales minus product cost minus ad spend) per product.

**Architecture:** New `GastoPublicidad` entity persisted in the existing PostgreSQL database (same tenant-scoped pattern as `Gasto`), following the codebase's existing layered structure (`Domain.Entities` → `Application.Interfaces`/`Application.Services` → `Infrastructure.Repositories` → `WEB_API.Controllers`). The `.xlsx` is parsed entirely in the browser with the `xlsx` library already installed; the backend only ever receives parsed JSON with a product already assigned per row.

**Tech Stack:** ASP.NET Core 6 / EF Core 6 / PostgreSQL (Npgsql) on the backend; React + Redux Toolkit + `xlsx` (SheetJS) on the frontend. No new dependencies on either side.

**Spec:** `docs/superpowers/specs/2026-08-25-roi-publicidad-facebook-design.md`

## Global Constraints

- Backend targets `.NET 6`, database is PostgreSQL via Npgsql — never SQL Server syntax.
- `EntityBase` auto-stamps `TenantId`, `FechaCreacion`, `UsuarioCreacion` in `SpaContext.SaveChangesAsync()` — never set these manually when creating an entity.
- Multi-tenant isolation is done via EF Core `HasQueryFilter(e => e.TenantId == _tenant.Name)` in `SpaContext.OnModelCreating` — never filter by `TenantId` manually in a repository query.
- Migrations apply automatically on app startup (`Program.cs` calls `context.Database.MigrateAsync()`) — never tell the executor to run `dotnet ef database update` manually.
- Follow the exact `Gasto`/`GastoRepository`/`GastoService`/`GastoController` pattern for every new file — same tuple-return repository signature `(ServiceStatus, T?, string)`, same `ErrorHandler` mapping in the service layer, same `Ok(await _service.X())` one-liner in the controller.
- No new NuGet or npm packages — `xlsx` is already installed in `Frontend/node_modules` (used today only in `ExportExcel.tsx`).
- **No automated test project exists anywhere in this repo (backend or frontend).** Per explicit user decision, this feature follows the same convention: manual verification only (via Swagger UI and the running app), no new xUnit/Jest/Vitest project introduced.
- New menu entry reuses the existing `code: "1300"` (the "Gastos" module code) instead of creating a new `AspNetModule`/`AspNetSubModule` row — same trick already used for the "Configuraciones" menu entry in `MData.ts`, avoids per-tenant permission plumbing for a first version.

---

### Task 1: `GastoPublicidad` entity, DbContext registration, migration

**Files:**
- Create: `Backend/Domain/Entities/GastoPublicidad.cs`
- Modify: `Backend/Infrastructure/Data/SpaContext.cs:72` (add `DbSet`), `Backend/Infrastructure/Data/SpaContext.cs:121` (add `HasQueryFilter`)
- Create (generated): `Backend/Infrastructure/Migrations/<timestamp>_AddGastoPublicidad.cs` and `.Designer.cs`, updates `Backend/Infrastructure/Migrations/SpaContextModelSnapshot.cs`

**Interfaces:**
- Produces: `GastoPublicidad` entity with `Id, ProductoId, Producto, NombreAnuncio, NombreConjuntoAnuncios, FechaInicio, FechaFin, ImporteGastado, Impresiones, Alcance, Resultados, CostoPorResultado, LoteImportacionId, HashAnuncio` — every later task's DTOs/repository code depends on these exact property names and types.

- [ ] **Step 1: Create the entity**

```csharp
// Backend/Domain/Entities/GastoPublicidad.cs
namespace Domain.Entities;

public class GastoPublicidad : EntityBase
{
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public string NombreAnuncio { get; set; } = null!;
    public string? NombreConjuntoAnuncios { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public decimal ImporteGastado { get; set; }

    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Resultados { get; set; }
    public decimal? CostoPorResultado { get; set; }

    public Guid LoteImportacionId { get; set; }

    public string HashAnuncio { get; set; } = null!;
}
```

- [ ] **Step 2: Register the DbSet**

In `Backend/Infrastructure/Data/SpaContext.cs`, right after line 72 (`public DbSet<Gasto> Gasto => Set<Gasto>();`), add:

```csharp
    public DbSet<GastoPublicidad> GastoPublicidad => Set<GastoPublicidad>();
```

- [ ] **Step 3: Register the tenant query filter**

In the same file, right after line 121 (`modelBuilder.Entity<Gasto>().HasQueryFilter(e => e.TenantId == _tenant.Name && (e.SucursalId == null || e.SucursalId == _tenant.SucursalId));`), add:

```csharp
        modelBuilder.Entity<GastoPublicidad>().HasQueryFilter(e => e.TenantId == _tenant.Name);
```

(No `SucursalId` on this entity — ad spend isn't tracked per branch, matching entities like `Metodopago`/`Proveedor` that only filter by `TenantId`.)

- [ ] **Step 4: Generate the migration**

Run from `Backend/`:

```bash
dotnet ef migrations add AddGastoPublicidad --project Infrastructure --startup-project WEB_API -o Migrations
```

- [ ] **Step 5: Verify it builds**

Run: `dotnet build WEB_API_SPA.sln` (from `Backend/`)
Expected: `0 Errores`, and the new migration file exists under `Backend/Infrastructure/Migrations/`.

- [ ] **Step 6: Commit**

```bash
git add Backend/Domain/Entities/GastoPublicidad.cs Backend/Infrastructure/Data/SpaContext.cs Backend/Infrastructure/Migrations/
git commit -m "feat: add GastoPublicidad entity and migration"
```

---

### Task 2: DTOs and payloads

**Files:**
- Create: `Backend/Domain/DTO/GastoPublicidadDto.cs`
- Create: `Backend/Domain/DTO/RoiPorProductoDto.cs`
- Create: `Backend/Domain/DTO/ImportarGastoPublicidadResultDto.cs`
- Create: `Backend/Domain/Payloads/GastoPublicidadFilaPayload.cs`
- Create: `Backend/Domain/Payloads/ImportarGastoPublicidadPayload.cs`
- Create: `Backend/Domain/Payloads/GastoPublicidadRoiQueryParams.cs`
- Create: `Backend/Domain/Payloads/GastoPublicidadQueryParams.cs`

**Interfaces:**
- Consumes: `GastoPublicidad` entity from Task 1.
- Produces: exact DTO/payload shapes every later backend task (repository, service, controller) and the frontend reducer depend on.

- [ ] **Step 1: Create the listing DTO**

```csharp
// Backend/Domain/DTO/GastoPublicidadDto.cs
namespace Domain.DTO;

public class GastoPublicidadDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public string NombreAnuncio { get; set; } = null!;
    public string? NombreConjuntoAnuncios { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal ImporteGastado { get; set; }
    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Resultados { get; set; }
    public decimal? CostoPorResultado { get; set; }
    public Guid LoteImportacionId { get; set; }
}
```

- [ ] **Step 2: Create the ROI-per-product DTO**

```csharp
// Backend/Domain/DTO/RoiPorProductoDto.cs
namespace Domain.DTO;

public class RoiPorProductoDto
{
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public decimal GastoAds { get; set; }
    public decimal Ingresos { get; set; }
    public decimal CostoProducto { get; set; }
    public decimal UtilidadNeta { get; set; }
    public decimal? RoiPorcentaje { get; set; }
}
```

- [ ] **Step 3: Create the import-result DTO**

```csharp
// Backend/Domain/DTO/ImportarGastoPublicidadResultDto.cs
namespace Domain.DTO;

public class ImportarGastoPublicidadResultDto
{
    public int FilasInsertadas { get; set; }
    public int FilasOmitidasPorDuplicado { get; set; }
}
```

- [ ] **Step 4: Create the import row payload**

```csharp
// Backend/Domain/Payloads/GastoPublicidadFilaPayload.cs
namespace Domain.Payloads;

public class GastoPublicidadFilaPayload
{
    public int ProductoId { get; set; }
    public string NombreAnuncio { get; set; } = null!;
    public string? NombreConjuntoAnuncios { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal ImporteGastado { get; set; }
    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Resultados { get; set; }
    public decimal? CostoPorResultado { get; set; }
}
```

- [ ] **Step 5: Create the import batch payload**

```csharp
// Backend/Domain/Payloads/ImportarGastoPublicidadPayload.cs
namespace Domain.Payloads;

public class ImportarGastoPublicidadPayload
{
    public Guid LoteImportacionId { get; set; }
    public List<GastoPublicidadFilaPayload> Filas { get; set; } = new();
}
```

- [ ] **Step 6: Create the ROI query params**

```csharp
// Backend/Domain/Payloads/GastoPublicidadRoiQueryParams.cs
namespace Domain.Payloads;

public class GastoPublicidadRoiQueryParams
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ProductoId { get; set; }
}
```

- [ ] **Step 7: Create the listing query params**

```csharp
// Backend/Domain/Payloads/GastoPublicidadQueryParams.cs
namespace Domain.Payloads;

public class GastoPublicidadQueryParams : PaginationPayload
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ProductoId { get; set; }
}
```

- [ ] **Step 8: Verify it builds**

Run: `dotnet build WEB_API_SPA.sln` (from `Backend/`)
Expected: `0 Errores`.

- [ ] **Step 9: Commit**

```bash
git add Backend/Domain/DTO/GastoPublicidadDto.cs Backend/Domain/DTO/RoiPorProductoDto.cs Backend/Domain/DTO/ImportarGastoPublicidadResultDto.cs Backend/Domain/Payloads/GastoPublicidadFilaPayload.cs Backend/Domain/Payloads/ImportarGastoPublicidadPayload.cs Backend/Domain/Payloads/GastoPublicidadRoiQueryParams.cs Backend/Domain/Payloads/GastoPublicidadQueryParams.cs
git commit -m "feat: add GastoPublicidad DTOs and payloads"
```

---

### Task 3: AutoMapper mapping

**Files:**
- Modify: `Backend/Domain/Common/Mappings/MyAutomapper.cs:126` (right after the `Gasto, GastoDto` mapping block)

**Interfaces:**
- Consumes: `GastoPublicidad` entity (Task 1), `GastoPublicidadDto` (Task 2).
- Produces: `IMapper` can `ProjectTo<GastoPublicidadDto>()` — Task 6's `Listar` repository method depends on this.

- [ ] **Step 1: Add the mapping**

In `Backend/Domain/Common/Mappings/MyAutomapper.cs`, right after the existing block:

```csharp
            CreateMap<Gasto, GastoDto>()
                .ForMember(x => x.MetodoPago, y => y.MapFrom(z => z.Metodopago != null ? z.Metodopago.Descripcion ?? z.Metodopago.Nombre : null))
                .ForMember(x => x.FechaGasto, y => y.MapFrom(z => z.FechaGasto.ToString("dd\MM\yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion));
```

add:

```csharp
            CreateMap<GastoPublicidad, GastoPublicidadDto>()
                .ForMember(x => x.NombreProducto, y => y.MapFrom(z => z.Producto != null ? z.Producto.Nombre : null));
```

- [ ] **Step 2: Verify it builds**

Run: `dotnet build WEB_API_SPA.sln` (from `Backend/`)
Expected: `0 Errores`.

- [ ] **Step 3: Commit**

```bash
git add Backend/Domain/Common/Mappings/MyAutomapper.cs
git commit -m "feat: map GastoPublicidad to GastoPublicidadDto"
```

---

### Task 4: Import endpoint (vertical slice — repository, service, controller, DI)

**Files:**
- Create: `Backend/Application/Interfaces/IRepository/IGastoPublicidadRepository.cs`
- Create: `Backend/Infrastructure/Repositories/GastoPublicidadRepository.cs`
- Create: `Backend/Application/Interfaces/IServices/IGastoPublicidadService.cs`
- Create: `Backend/Application/Services/GastoPublicidadService.cs`
- Create: `Backend/WEB_API/Controllers/GastoPublicidadController.cs`
- Modify: `Backend/Infrastructure/DependencyInjection.cs:36` and `:62`

**Interfaces:**
- Consumes: `GastoPublicidad`/`Producto` entities, `ImportarGastoPublicidadPayload`, `GastoPublicidadFilaPayload`, `ImportarGastoPublicidadResultDto` (Tasks 1-2).
- Produces: `POST /api/gastopublicidad/importar`. `IGastoPublicidadRepository.Importar(ImportarGastoPublicidadPayload) -> Task<(ServiceStatus, ImportarGastoPublicidadResultDto?, string)>` — this exact interface gets two more methods appended in Tasks 5 and 6.

- [ ] **Step 1: Create the repository interface**

```csharp
// Backend/Application/Interfaces/IRepository/IGastoPublicidadRepository.cs
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IGastoPublicidadRepository
{
    Task<(ServiceStatus, ImportarGastoPublicidadResultDto?, string)> Importar(ImportarGastoPublicidadPayload payload);
}
```

- [ ] **Step 2: Create the repository implementation**

```csharp
// Backend/Infrastructure/Repositories/GastoPublicidadRepository.cs
using Application.Interfaces.IRepository;
using AutoMapper;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Repositories;

public class GastoPublicidadRepository : IGastoPublicidadRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;

    public GastoPublicidadRepository(SpaContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(ServiceStatus, ImportarGastoPublicidadResultDto?, string)> Importar(ImportarGastoPublicidadPayload payload)
    {
        if (payload.Filas == null || payload.Filas.Count == 0)
            return (ServiceStatus.FailedValidation, null, "No hay filas para importar");

        var productoIds = payload.Filas.Select(f => f.ProductoId).Distinct().ToList();
        var productosExistentes = await _context.Producto.AsNoTracking()
            .Where(p => productoIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var errores = new List<string>();
        var candidatas = new List<GastoPublicidad>();

        for (var i = 0; i < payload.Filas.Count; i++)
        {
            var fila = payload.Filas[i];

            if (!productosExistentes.Contains(fila.ProductoId))
            {
                errores.Add($"Fila {i + 1}: el producto {fila.ProductoId} no existe");
                continue;
            }

            if (fila.FechaFin < fila.FechaInicio)
            {
                errores.Add($"Fila {i + 1}: la fecha fin debe ser posterior o igual a la fecha inicio");
                continue;
            }

            if (fila.ImporteGastado < 0)
            {
                errores.Add($"Fila {i + 1}: el importe gastado no puede ser negativo");
                continue;
            }

            candidatas.Add(new GastoPublicidad
            {
                ProductoId = fila.ProductoId,
                NombreAnuncio = fila.NombreAnuncio,
                NombreConjuntoAnuncios = fila.NombreConjuntoAnuncios,
                FechaInicio = fila.FechaInicio,
                FechaFin = fila.FechaFin,
                ImporteGastado = fila.ImporteGastado,
                Impresiones = fila.Impresiones,
                Alcance = fila.Alcance,
                Resultados = fila.Resultados,
                CostoPorResultado = fila.CostoPorResultado,
                LoteImportacionId = payload.LoteImportacionId,
                HashAnuncio = CalcularHash(fila.NombreAnuncio, fila.FechaInicio, fila.FechaFin)
            });
        }

        if (errores.Count > 0)
            return (ServiceStatus.FailedValidation, null, string.Join(" | ", errores));

        try
        {
            var hashes = candidatas.Select(c => c.HashAnuncio).ToList();
            var hashesExistentes = await _context.GastoPublicidad.AsNoTracking()
                .Where(g => hashes.Contains(g.HashAnuncio))
                .Select(g => g.HashAnuncio)
                .ToListAsync();

            var aInsertar = candidatas.Where(c => !hashesExistentes.Contains(c.HashAnuncio)).ToList();
            var omitidas = candidatas.Count - aInsertar.Count;

            if (aInsertar.Count > 0)
            {
                await _context.GastoPublicidad.AddRangeAsync(aInsertar);
                await _context.SaveChangesAsync();
            }

            var resultado = new ImportarGastoPublicidadResultDto
            {
                FilasInsertadas = aInsertar.Count,
                FilasOmitidasPorDuplicado = omitidas
            };

            return (ServiceStatus.Ok, resultado, "Importación completada");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al importar publicidad -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    private static string CalcularHash(string nombreAnuncio, DateTime fechaInicio, DateTime fechaFin)
    {
        var input = $"{nombreAnuncio}|{fechaInicio:O}|{fechaFin:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
```

(No DB-level unique index on `HashAnuncio` — this is a low-concurrency admin import action, so the pre-insert check above is the whole duplicate-detection mechanism. A concurrent double-submit racing past this check is an accepted simplification, not a case worth defending against here.)

- [ ] **Step 3: Create the service interface**

```csharp
// Backend/Application/Interfaces/IServices/IGastoPublicidadService.cs
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IGastoPublicidadService
{
    Task<MessageResult<object>> Importar(ImportarGastoPublicidadPayload payload);
}
```

- [ ] **Step 4: Create the service implementation**

```csharp
// Backend/Application/Services/GastoPublicidadService.cs
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class GastoPublicidadService : IGastoPublicidadService
{
    private readonly IGastoPublicidadRepository _repository;

    public GastoPublicidadService(IGastoPublicidadRepository repository)
    {
        _repository = repository;
    }

    public async Task<MessageResult<object>> Importar(ImportarGastoPublicidadPayload payload)
    {
        var (estado, result, message) = await _repository.Importar(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, result);

        return MessageResult<object>.Of(message, result);
    }
}
```

- [ ] **Step 5: Create the controller**

```csharp
// Backend/WEB_API/Controllers/GastoPublicidadController.cs
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/gastopublicidad")]
[ApiController]
public class GastoPublicidadController : ControllerBase
{
    private readonly IGastoPublicidadService _gastoPublicidadService;

    public GastoPublicidadController(IGastoPublicidadService gastoPublicidadService)
    {
        _gastoPublicidadService = gastoPublicidadService;
    }

    [HttpPost("importar")]
    public async Task<IActionResult> Importar([FromBody] ImportarGastoPublicidadPayload payload) => Ok(await _gastoPublicidadService.Importar(payload));
}
```

- [ ] **Step 6: Register in DI**

In `Backend/Infrastructure/DependencyInjection.cs`, right after line 36 (`.AddScoped<IGastoRepository, GastoRepository>()`), add:

```csharp
                    .AddScoped<IGastoPublicidadRepository, GastoPublicidadRepository>()
```

Right after line 62 (`.AddScoped<IGastoService, GastoService>()`), add:

```csharp
                    .AddScoped<IGastoPublicidadService, GastoPublicidadService>()
```

- [ ] **Step 7: Verify it builds**

Run: `dotnet build WEB_API_SPA.sln` (from `Backend/`)
Expected: `0 Errores`.

- [ ] **Step 8: Manual verification**

Run: `dotnet run --project WEB_API` (from `Backend/`)
Open the Swagger UI at the app's root URL, `Authorize` with a bearer token obtained from `POST /api/autenticacion/token` (use an existing test user), then call `POST /api/gastopublicidad/importar` with a body like:

```json
{
  "loteImportacionId": "550e8400-e29b-41d4-a716-446655440000",
  "filas": [
    {
      "productoId": 1,
      "nombreAnuncio": "Test anuncio",
      "fechaInicio": "2026-08-01",
      "fechaFin": "2026-08-15",
      "importeGastado": 100.50
    }
  ]
}
```

(replace `productoId: 1` with a real product id from your database)

Expected: `200 OK` with `{"filasInsertadas": 1, "filasOmitidasPorDuplicado": 0}` in `data`. Calling it again with the same body should return `{"filasInsertadas": 0, "filasOmitidasPorDuplicado": 1}`.

- [ ] **Step 9: Commit**

```bash
git add Backend/Application/Interfaces/IRepository/IGastoPublicidadRepository.cs Backend/Infrastructure/Repositories/GastoPublicidadRepository.cs Backend/Application/Interfaces/IServices/IGastoPublicidadService.cs Backend/Application/Services/GastoPublicidadService.cs Backend/WEB_API/Controllers/GastoPublicidadController.cs Backend/Infrastructure/DependencyInjection.cs
git commit -m "feat: add GastoPublicidad import endpoint"
```

---

### Task 5: ROI endpoint

**Files:**
- Modify: `Backend/Application/Interfaces/IRepository/IGastoPublicidadRepository.cs`
- Modify: `Backend/Infrastructure/Repositories/GastoPublicidadRepository.cs`
- Modify: `Backend/Application/Interfaces/IServices/IGastoPublicidadService.cs`
- Modify: `Backend/Application/Services/GastoPublicidadService.cs`
- Modify: `Backend/WEB_API/Controllers/GastoPublicidadController.cs`

**Interfaces:**
- Consumes: `GastoPublicidadRoiQueryParams`, `RoiPorProductoDto` (Task 2), `ComprobanteDetalle`/`ComprobanteCabecera`/`EstatusComprobante.Anulado` (existing entities).
- Produces: `GET /api/gastopublicidad/roi`. `IGastoPublicidadRepository.CalcularRoi(GastoPublicidadRoiQueryParams) -> Task<(ServiceStatus, List<RoiPorProductoDto>?, string)>`.

- [ ] **Step 1: Add the method to the repository interface**

```csharp
    Task<(ServiceStatus, List<RoiPorProductoDto>?, string)> CalcularRoi(GastoPublicidadRoiQueryParams payload);
```

(append inside `IGastoPublicidadRepository`, after `Importar`)

- [ ] **Step 2: Implement it in the repository**

Add `using Domain.Enumerations;` to the top of `GastoPublicidadRepository.cs` (needed for `EstatusComprobante.Anulado`, same as `DashboardRepository.cs` imports it), then append this method to the class:

```csharp
    public async Task<(ServiceStatus, List<RoiPorProductoDto>?, string)> CalcularRoi(GastoPublicidadRoiQueryParams payload)
    {
        try
        {
            var query = _context.GastoPublicidad.AsNoTracking().Include(g => g.Producto).AsQueryable();

            if (payload.Desde.HasValue)
                query = query.Where(g => g.FechaFin >= payload.Desde.Value);

            if (payload.Hasta.HasValue)
                query = query.Where(g => g.FechaInicio <= payload.Hasta.Value);

            if (payload.ProductoId.HasValue)
                query = query.Where(g => g.ProductoId == payload.ProductoId.Value);

            var ads = await query.ToListAsync();

            if (ads.Count == 0)
                return (ServiceStatus.Ok, new List<RoiPorProductoDto>(), "Sin datos para el rango seleccionado");

            var resultado = new List<RoiPorProductoDto>();

            foreach (var grupo in ads.GroupBy(a => a.ProductoId))
            {
                var minFecha = grupo.Min(a => a.FechaInicio);
                var maxFecha = grupo.Max(a => a.FechaFin);

                var detalles = await _context.ComprobanteDetalle.AsNoTracking()
                    .Where(d => d.ProductoId == grupo.Key
                             && d.ComprobanteCabecera.FechaCreacion >= minFecha
                             && d.ComprobanteCabecera.FechaCreacion <= maxFecha
                             && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                    .Select(d => new
                    {
                        d.Cantidad,
                        d.ValorUnitarioTotal,
                        FechaVenta = d.ComprobanteCabecera.FechaCreacion,
                        CostoUnitario = d.Producto.CostoUnitario
                    })
                    .ToListAsync();

                // Cada venta cuenta una sola vez para el producto aunque caiga dentro de
                // varios anuncios de ese mismo producto que se solapan en fechas —
                // sumarla más de una vez inflaría el ingreso de una sola fila del reporte.
                var ventasEnRango = detalles
                    .Where(d => grupo.Any(a => d.FechaVenta >= a.FechaInicio && d.FechaVenta <= a.FechaFin))
                    .ToList();

                var gastoAds = grupo.Sum(a => a.ImporteGastado);
                var ingresos = ventasEnRango.Sum(d => d.ValorUnitarioTotal);
                var costoProducto = ventasEnRango.Sum(d => d.Cantidad * (d.CostoUnitario ?? 0));
                var utilidadNeta = ingresos - costoProducto - gastoAds;

                resultado.Add(new RoiPorProductoDto
                {
                    ProductoId = grupo.Key,
                    NombreProducto = grupo.First().Producto.Nombre,
                    GastoAds = gastoAds,
                    Ingresos = ingresos,
                    CostoProducto = costoProducto,
                    UtilidadNeta = utilidadNeta,
                    RoiPorcentaje = gastoAds > 0 ? utilidadNeta / gastoAds : (decimal?)null
                });
            }

            return (ServiceStatus.Ok, resultado.OrderByDescending(r => r.RoiPorcentaje ?? decimal.MinValue).ToList(), "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al calcular ROI -> {e.InnerException?.Message ?? e.Message}");
        }
    }
```

- [ ] **Step 3: Add the method to the service interface**

```csharp
    Task<MessageResult<object>> CalcularRoi(GastoPublicidadRoiQueryParams payload);
```

(append inside `IGastoPublicidadService`)

- [ ] **Step 4: Implement it in the service**

```csharp
    public async Task<MessageResult<object>> CalcularRoi(GastoPublicidadRoiQueryParams payload)
    {
        var (estado, result, message) = await _repository.CalcularRoi(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }
```

(append inside `GastoPublicidadService`)

- [ ] **Step 5: Add the controller action**

```csharp
    [HttpGet("roi")]
    public async Task<IActionResult> CalcularRoi([FromQuery] GastoPublicidadRoiQueryParams payload) => Ok(await _gastoPublicidadService.CalcularRoi(payload));
```

(append inside `GastoPublicidadController`)

- [ ] **Step 6: Verify it builds**

Run: `dotnet build WEB_API_SPA.sln` (from `Backend/`)
Expected: `0 Errores`.

- [ ] **Step 7: Manual verification**

With the app running (Task 4's Step 8), call `GET /api/gastopublicidad/roi?desde=2026-08-01&hasta=2026-08-31` via Swagger UI.
Expected: `200 OK` with an array in `data`, one entry per product that has `GastoPublicidad` rows in that range, each with `gastoAds`, `ingresos`, `costoProducto`, `utilidadNeta`, `roiPorcentaje`.

- [ ] **Step 8: Commit**

```bash
git add Backend/Application/Interfaces/IRepository/IGastoPublicidadRepository.cs Backend/Infrastructure/Repositories/GastoPublicidadRepository.cs Backend/Application/Interfaces/IServices/IGastoPublicidadService.cs Backend/Application/Services/GastoPublicidadService.cs Backend/WEB_API/Controllers/GastoPublicidadController.cs
git commit -m "feat: add GastoPublicidad ROI calculation endpoint"
```

---

### Task 6: Listing endpoint

**Files:**
- Modify: `Backend/Application/Interfaces/IRepository/IGastoPublicidadRepository.cs`
- Modify: `Backend/Infrastructure/Repositories/GastoPublicidadRepository.cs`
- Modify: `Backend/Application/Interfaces/IServices/IGastoPublicidadService.cs`
- Modify: `Backend/Application/Services/GastoPublicidadService.cs`
- Modify: `Backend/WEB_API/Controllers/GastoPublicidadController.cs`

**Interfaces:**
- Consumes: `GastoPublicidadQueryParams`, `GastoPublicidadDto` (Task 2), `DataCollection<T>`/`GetPagedAsync` (existing `Domain.Common`/`Domain.Common.PaggingEntention`).
- Produces: `GET /api/gastopublicidad/listar`.

- [ ] **Step 1: Add the method to the repository interface**

```csharp
    Task<(ServiceStatus, DataCollection<GastoPublicidadDto>?, string)> Listar(GastoPublicidadQueryParams payload);
```

(append inside `IGastoPublicidadRepository`; add `using Domain.Common;` at the top of the file if not already present)

- [ ] **Step 2: Implement it in the repository**

Add `using AutoMapper.QueryableExtensions;` and `using Domain.Common;` to the top of `GastoPublicidadRepository.cs`, then append:

```csharp
    public async Task<(ServiceStatus, DataCollection<GastoPublicidadDto>?, string)> Listar(GastoPublicidadQueryParams payload)
    {
        try
        {
            var query = _context.GastoPublicidad.AsNoTracking().Include(g => g.Producto).AsQueryable();

            if (payload.ProductoId.HasValue)
                query = query.Where(g => g.ProductoId == payload.ProductoId.Value);

            if (payload.Desde.HasValue)
                query = query.Where(g => g.FechaFin >= payload.Desde.Value);

            if (payload.Hasta.HasValue)
                query = query.Where(g => g.FechaInicio <= payload.Hasta.Value);

            var lista = await query.OrderByDescending(g => g.Id)
                                   .ProjectTo<GastoPublicidadDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar publicidad -> {e.InnerException?.Message ?? e.Message}");
        }
    }
```

- [ ] **Step 3: Add the method to the service interface**

```csharp
    Task<MessageResult<object>> Listar(GastoPublicidadQueryParams payload);
```

(append inside `IGastoPublicidadService`)

- [ ] **Step 4: Implement it in the service**

```csharp
    public async Task<MessageResult<object>> Listar(GastoPublicidadQueryParams payload)
    {
        var (estado, result, message) = await _repository.Listar(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, message, result);

        return MessageResult<object>.Of(message, result);
    }
```

(append inside `GastoPublicidadService`)

- [ ] **Step 5: Add the controller action**

```csharp
    [HttpGet("listar")]
    public async Task<IActionResult> Listar([FromQuery] GastoPublicidadQueryParams payload) => Ok(await _gastoPublicidadService.Listar(payload));
```

(append inside `GastoPublicidadController`)

- [ ] **Step 6: Verify it builds**

Run: `dotnet build WEB_API_SPA.sln` (from `Backend/`)
Expected: `0 Errores`.

- [ ] **Step 7: Manual verification**

With the app running, call `GET /api/gastopublicidad/listar?Page=1&Amount=10` via Swagger UI.
Expected: `200 OK` with `{items: [...], total, page, pages}` in `data`, containing the rows inserted in Task 4's verification.

- [ ] **Step 8: Commit**

```bash
git add Backend/Application/Interfaces/IRepository/IGastoPublicidadRepository.cs Backend/Infrastructure/Repositories/GastoPublicidadRepository.cs Backend/Application/Interfaces/IServices/IGastoPublicidadService.cs Backend/Application/Services/GastoPublicidadService.cs Backend/WEB_API/Controllers/GastoPublicidadController.cs
git commit -m "feat: add GastoPublicidad listing endpoint"
```

---

### Task 7: Frontend Redux reducer

**Files:**
- Create: `Frontend/src/redux/reducers/Admin/gastoPublicidad/types/index.tsx`
- Create: `Frontend/src/redux/reducers/Admin/gastoPublicidad/interfaces/index.tsx`
- Create: `Frontend/src/redux/reducers/Admin/gastoPublicidad/gastoPublicidad.reducer.ts`
- Modify: `Frontend/src/redux/rootState.ts`
- Modify: `Frontend/src/redux/store.ts`

**Interfaces:**
- Consumes: `axiosInstance` from `Frontend/src/utils/axios.ts`; backend routes from Tasks 4-6 (`/gastopublicidad/importar`, `/gastopublicidad/roi`).
- Produces: `state.publicidad.roi: IRoiPorProducto[]`, `getRoiPublicidad(desde?, hasta?, productoId?)` thunk, `importarGastoPublicidad(payload)` async function — Task 9's screen component depends on these exact names.

- [ ] **Step 1: Create the action type**

```tsx
// Frontend/src/redux/reducers/Admin/gastoPublicidad/types/index.tsx
export const GET_GASTO_PUBLICIDAD_ROI = 'GET_GASTO_PUBLICIDAD_ROI';
```

- [ ] **Step 2: Create the state interfaces**

```tsx
// Frontend/src/redux/reducers/Admin/gastoPublicidad/interfaces/index.tsx
export interface IRoiPorProducto {
  productoId: number;
  nombreProducto: string;
  gastoAds: number;
  ingresos: number;
  costoProducto: number;
  utilidadNeta: number;
  roiPorcentaje: number | null;
}

export interface IGastoPublicidadState {
  roi: IRoiPorProducto[];
}
```

- [ ] **Step 3: Create the reducer**

```tsx
// Frontend/src/redux/reducers/Admin/gastoPublicidad/gastoPublicidad.reducer.ts
import { createReducer, Dispatch, AnyAction } from "@reduxjs/toolkit";
import * as types from "./types";
import { IGastoPublicidadState } from "./interfaces";
import axiosInstance from "../../../../utils/axios";
import { toast } from "sonner";

const initialState: IGastoPublicidadState = {
  roi: [],
};

export const gastoPublicidadReducer = createReducer(initialState, (builder) => {
  builder.addCase(
    types.GET_GASTO_PUBLICIDAD_ROI,
    (state: IGastoPublicidadState, action: any): IGastoPublicidadState => {
      return {
        ...state,
        roi: action.payload,
      };
    }
  );
});

export const getRoiPublicidad = (desde?: string, hasta?: string, productoId?: number) => {
  return async (dispatch: Dispatch<AnyAction>) => {
    try {
      const params = new URLSearchParams();
      if (desde) params.append("Desde", desde);
      if (hasta) params.append("Hasta", hasta);
      if (productoId) params.append("ProductoId", String(productoId));

      const response: any = await axiosInstance.get(`/gastopublicidad/roi?${params.toString()}`);
      const { status, data } = response;
      if (status === 200) {
        dispatch({ type: types.GET_GASTO_PUBLICIDAD_ROI, payload: data?.data ?? [] });
      }
    } catch (error: any) {
      console.log(error);
      toast.error(error?.response?.data?.message ?? "Error al calcular el ROI");
      dispatch({ type: types.GET_GASTO_PUBLICIDAD_ROI, payload: [] });
    }
  };
};

export const importarGastoPublicidad = async (payload: any) => {
  const response: any = await axiosInstance.post("/gastopublicidad/importar", payload);
  return response.data?.data;
};
```

- [ ] **Step 4: Register the reducer in `rootState.ts`**

In `Frontend/src/redux/rootState.ts`, add `publicidad: any;` to the `RootState` interface (right after the `gastos: any;` line).

- [ ] **Step 5: Register the reducer in `store.ts`**

In `Frontend/src/redux/store.ts`, add the import right after the `gastoReducer` import:

```ts
import { gastoPublicidadReducer } from './reducers/Admin/gastoPublicidad/gastoPublicidad.reducer';
```

and add `publicidad: gastoPublicidadReducer,` right after `gastos: gastoReducer,` inside `configureStore`.

- [ ] **Step 6: Verify it builds**

Run: `npm run build` (from `Frontend/`)
Expected: build succeeds with no TypeScript errors.

- [ ] **Step 7: Commit**

```bash
git add src/redux/reducers/Admin/gastoPublicidad/ src/redux/rootState.ts src/redux/store.ts
git commit -m "feat: add GastoPublicidad redux reducer"
```

---

### Task 8: Excel parser helper

**Files:**
- Create: `Frontend/src/helpers/functions/parseGastoPublicidadExcel.ts`

**Interfaces:**
- Consumes: `xlsx` (`XLSX.read`, `XLSX.utils.sheet_to_json`) — already installed.
- Produces: `parseGastoPublicidadExcel(arrayBuffer: ArrayBuffer): { filas: IFilaPublicidadParseada[]; errores: string[] }` — Task 9's screen component depends on this exact signature and the `IFilaPublicidadParseada` shape.

- [ ] **Step 1: Write the parser**

```ts
// Frontend/src/helpers/functions/parseGastoPublicidadExcel.ts
import * as XLSX from "xlsx";

export interface IFilaPublicidadParseada {
  nombreAnuncio: string;
  nombreConjuntoAnuncios: string | null;
  fechaInicio: string; // yyyy-MM-dd
  fechaFin: string;
  importeGastado: number;
  impresiones: number | null;
  alcance: number | null;
  resultados: number | null;
  costoPorResultado: number | null;
  productoId: number | null;
}

export interface IParseoResultado {
  filas: IFilaPublicidadParseada[];
  errores: string[];
}

const COLUMNAS_REQUERIDAS = [
  "Inicio del informe",
  "Fin del informe",
  "Nombre del anuncio",
  "Importe gastado (PEN)",
];

function excelFechaAIso(valor: any): string | null {
  if (!valor) return null;
  const fecha = valor instanceof Date ? valor : new Date(valor);
  return isNaN(fecha.getTime()) ? null : fecha.toISOString().slice(0, 10);
}

export function parseGastoPublicidadExcel(arrayBuffer: ArrayBuffer): IParseoResultado {
  const workbook = XLSX.read(arrayBuffer, { type: "array" });
  const sheet = workbook.Sheets[workbook.SheetNames[0]];
  const filasCrudas: any[] = XLSX.utils.sheet_to_json(sheet, { raw: false });

  if (filasCrudas.length === 0) {
    return { filas: [], errores: ["El archivo no tiene filas de datos."] };
  }

  const columnasFaltantes = COLUMNAS_REQUERIDAS.filter(
    (col) => !Object.prototype.hasOwnProperty.call(filasCrudas[0], col)
  );
  if (columnasFaltantes.length > 0) {
    return {
      filas: [],
      errores: [`Faltan columnas requeridas en el Excel: ${columnasFaltantes.join(", ")}`],
    };
  }

  const filas: IFilaPublicidadParseada[] = [];
  const errores: string[] = [];

  filasCrudas.forEach((fila, index) => {
    const nombreAnuncio = fila["Nombre del anuncio"];
    const fechaInicio = excelFechaAIso(fila["Inicio del informe"]);
    const fechaFin = excelFechaAIso(fila["Fin del informe"]);
    const importeGastado = parseFloat(fila["Importe gastado (PEN)"]);

    if (!nombreAnuncio || !fechaInicio || !fechaFin || isNaN(importeGastado)) {
      errores.push(`Fila ${index + 2} del Excel: datos incompletos, se descarta.`);
      return;
    }

    filas.push({
      nombreAnuncio,
      nombreConjuntoAnuncios: fila["Nombre del conjunto de anuncios"] ?? null,
      fechaInicio,
      fechaFin,
      importeGastado,
      impresiones: fila["Impresiones"] ? parseInt(fila["Impresiones"], 10) : null,
      alcance: fila["Alcance"] ? parseInt(fila["Alcance"], 10) : null,
      resultados: fila["Compras"] ? parseInt(fila["Compras"], 10) : null,
      costoPorResultado: fila["Costo por compra (PEN)"] ? parseFloat(fila["Costo por compra (PEN)"]) : null,
      productoId: null,
    });
  });

  return { filas, errores };
}
```

- [ ] **Step 2: Verify it builds**

Run: `npm run build` (from `Frontend/`)
Expected: build succeeds with no TypeScript errors.

- [ ] **Step 3: Manual verification**

In a scratch file or the browser console (after Task 9 wires it into the UI), upload a real Meta Ads Excel export and confirm `filas` has one entry per ad row with the right `nombreAnuncio`/`importeGastado`/dates, and that a row missing "Importe gastado (PEN)" ends up in `errores` instead of `filas`.

- [ ] **Step 4: Commit**

```bash
git add src/helpers/functions/parseGastoPublicidadExcel.ts
git commit -m "feat: add Meta Ads Excel parser for GastoPublicidad"
```

---

### Task 9: Screen component (upload, preview, mapping, confirm, ROI summary)

**Files:**
- Create: `Frontend/src/presentation/views/Modules/Admin/Views/GastoPublicidad/index.tsx`
- Create: `Frontend/src/presentation/views/Modules/Admin/Views/GastoPublicidad/gastoPublicidad.module.css`

**Interfaces:**
- Consumes: `parseGastoPublicidadExcel` (Task 8), `getRoiPublicidad`/`importarGastoPublicidad` (Task 7), `axiosInstance` (existing), `GET /productos/listar` (existing endpoint).
- Produces: `GastoPublicidad` React component — Task 10's router wires this in.

- [ ] **Step 1: Write the CSS module**

```css
/* Frontend/src/presentation/views/Modules/Admin/Views/GastoPublicidad/gastoPublicidad.module.css */
.header {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  margin-bottom: 20px;
}

.header h3 {
  font-size: 18px;
  font-weight: 600;
}

.newBtn {
  background: #2997FE;
  color: #fff;
  border: 0;
  border-radius: 6px;
  padding: 10px 18px;
  cursor: pointer;
  font-size: 14px;
  display: inline-block;
}

.newBtn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.tableWrap {
  overflow-x: auto;
  background: #fff;
  border-radius: 12px;
  border: 1px solid #F1F1F2;
  margin-bottom: 20px;
}

.table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.table th {
  text-align: left;
  color: #99a1b7;
  text-transform: uppercase;
  font-size: 11px;
  padding: 12px 14px;
  border-bottom: 1px solid #f1f1f2;
  white-space: nowrap;
}

.table td {
  padding: 12px 14px;
  border-bottom: 1px solid #f7f7f8;
  white-space: nowrap;
}

.empty {
  text-align: center;
  color: #99a1b7;
  padding: 30px 10px !important;
}

.errores {
  background: #FDECEE;
  color: #F24B89;
  border-radius: 8px;
  padding: 10px 14px;
  margin-bottom: 16px;
  font-size: 13px;
}

.filtros {
  display: flex;
  gap: 10px;
  align-items: center;
  margin-bottom: 16px;
}

.positivo {
  color: #17B26A;
  font-weight: 600;
}

.negativo {
  color: #F24B89;
  font-weight: 600;
}
```

- [ ] **Step 2: Write the component**

```tsx
// Frontend/src/presentation/views/Modules/Admin/Views/GastoPublicidad/index.tsx
import { useEffect, useState } from "react";
import styles from "./gastoPublicidad.module.css";
import { Toaster, toast } from "sonner";
import { useAppDispatch, useAppSelector } from "../../../../../../redux/store";
import { RootState } from "../../../../../../redux/rootState";
import {
  getRoiPublicidad,
  importarGastoPublicidad,
} from "../../../../../../redux/reducers/Admin/gastoPublicidad/gastoPublicidad.reducer";
import {
  parseGastoPublicidadExcel,
  IFilaPublicidadParseada,
} from "../../../../../../helpers/functions/parseGastoPublicidadExcel";
import axiosInstance from "../../../../../../utils/axios";

interface IProductoOpcion {
  productoId: number;
  nombre: string;
}

export const GastoPublicidad = () => {
  const dispatch = useAppDispatch();
  const { roi }: any = useAppSelector((state: RootState) => state.publicidad);

  const [productos, setProductos] = useState<IProductoOpcion[]>([]);
  const [filas, setFilas] = useState<IFilaPublicidadParseada[]>([]);
  const [errores, setErrores] = useState<string[]>([]);
  const [subiendo, setSubiendo] = useState(false);
  const [desde, setDesde] = useState("");
  const [hasta, setHasta] = useState("");

  useEffect(() => {
    axiosInstance
      .get("/productos/listar?Amount=1000")
      .then((res: any) => {
        const items = res.data?.data?.items ?? [];
        setProductos(items.map((p: any) => ({ productoId: p.productoId, nombre: p.nombre })));
      })
      .catch(() => setProductos([]));
  }, []);

  useEffect(() => {
    dispatch(getRoiPublicidad() as any);
  }, [dispatch]);

  const handleFile = async (file: File) => {
    const buffer = await file.arrayBuffer();
    const { filas: parseadas, errores: erroresParseo } = parseGastoPublicidadExcel(buffer);
    setFilas(parseadas);
    setErrores(erroresParseo);
  };

  const handleProductoChange = (index: number, productoId: number) => {
    setFilas((prev) => prev.map((f, i) => (i === index ? { ...f, productoId } : f)));
  };

  const puedeConfirmar = filas.length > 0 && filas.every((f) => f.productoId !== null);

  const handleConfirmar = async () => {
    setSubiendo(true);
    try {
      const resultado = await importarGastoPublicidad({
        loteImportacionId: crypto.randomUUID(),
        filas: filas.map((f) => ({
          productoId: f.productoId,
          nombreAnuncio: f.nombreAnuncio,
          nombreConjuntoAnuncios: f.nombreConjuntoAnuncios,
          fechaInicio: f.fechaInicio,
          fechaFin: f.fechaFin,
          importeGastado: f.importeGastado,
          impresiones: f.impresiones,
          alcance: f.alcance,
          resultados: f.resultados,
          costoPorResultado: f.costoPorResultado,
        })),
      });

      toast.success(
        `Importación completada: ${resultado.filasInsertadas} filas insertadas` +
          (resultado.filasOmitidasPorDuplicado > 0
            ? `, ${resultado.filasOmitidasPorDuplicado} omitidas por duplicado`
            : "")
      );

      setFilas([]);
      setErrores([]);
      dispatch(getRoiPublicidad(desde || undefined, hasta || undefined) as any);
    } catch (error: any) {
      toast.error(error?.response?.data?.message ?? "Error al importar el archivo");
    } finally {
      setSubiendo(false);
    }
  };

  const handleFiltrar = () => {
    dispatch(getRoiPublicidad(desde || undefined, hasta || undefined) as any);
  };

  return (
    <div>
      <div className={styles.header}>
        <h3>ROI Publicidad (Meta Ads)</h3>
        <label className={styles.newBtn}>
          Subir Excel
          <input
            type="file"
            accept=".xlsx"
            style={{ display: "none" }}
            onChange={(e) => e.target.files?.[0] && handleFile(e.target.files[0])}
          />
        </label>
      </div>

      {errores.length > 0 && (
        <div className={styles.errores}>
          {errores.map((e, i) => (
            <p key={i}>{e}</p>
          ))}
        </div>
      )}

      {filas.length > 0 && (
        <div className={styles.tableWrap}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Anuncio</th>
                <th>Conjunto</th>
                <th>Inicio</th>
                <th>Fin</th>
                <th>Gasto (PEN)</th>
                <th>Producto</th>
              </tr>
            </thead>
            <tbody>
              {filas.map((f, i) => (
                <tr key={i}>
                  <td>{f.nombreAnuncio}</td>
                  <td>{f.nombreConjuntoAnuncios ?? "-"}</td>
                  <td>{f.fechaInicio}</td>
                  <td>{f.fechaFin}</td>
                  <td>S/ {f.importeGastado.toFixed(2)}</td>
                  <td>
                    <select
                      value={f.productoId ?? ""}
                      onChange={(e) => handleProductoChange(i, Number(e.target.value))}
                    >
                      <option value="">Selecciona un producto</option>
                      {productos.map((p) => (
                        <option key={p.productoId} value={p.productoId}>
                          {p.nombre}
                        </option>
                      ))}
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ padding: "12px 14px" }}>
            <button
              className={styles.newBtn}
              disabled={!puedeConfirmar || subiendo}
              onClick={handleConfirmar}
            >
              {subiendo ? "Importando..." : "Confirmar importación"}
            </button>
          </div>
        </div>
      )}

      <div className={styles.filtros}>
        <input type="date" value={desde} onChange={(e) => setDesde(e.target.value)} />
        <input type="date" value={hasta} onChange={(e) => setHasta(e.target.value)} />
        <button className={styles.newBtn} onClick={handleFiltrar}>
          Filtrar
        </button>
      </div>

      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Producto</th>
              <th>Gasto Ads</th>
              <th>Ingresos</th>
              <th>Costo Producto</th>
              <th>Utilidad Neta</th>
              <th>ROI %</th>
            </tr>
          </thead>
          <tbody>
            {!roi || roi.length === 0 ? (
              <tr>
                <td colSpan={6} className={styles.empty}>
                  No hay datos de ROI para este rango.
                </td>
              </tr>
            ) : (
              roi.map((r: any) => (
                <tr key={r.productoId}>
                  <td>{r.nombreProducto}</td>
                  <td>S/ {r.gastoAds.toFixed(2)}</td>
                  <td>S/ {r.ingresos.toFixed(2)}</td>
                  <td>S/ {r.costoProducto.toFixed(2)}</td>
                  <td className={r.utilidadNeta >= 0 ? styles.positivo : styles.negativo}>
                    S/ {r.utilidadNeta.toFixed(2)}
                  </td>
                  <td className={r.utilidadNeta >= 0 ? styles.positivo : styles.negativo}>
                    {r.roiPorcentaje === null ? "—" : `${(r.roiPorcentaje * 100).toFixed(0)}%`}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Toaster richColors position="top-right" duration={2000} />
    </div>
  );
};
```

- [ ] **Step 3: Verify it builds**

Run: `npm run build` (from `Frontend/`)
Expected: build succeeds with no TypeScript errors.

- [ ] **Step 4: Commit**

```bash
git add src/presentation/views/Modules/Admin/Views/GastoPublicidad/
git commit -m "feat: add GastoPublicidad screen (upload, preview, ROI summary)"
```

---

### Task 10: Menu entry and route registration

**Files:**
- Modify: `Frontend/src/infraestructure/MData/MData.ts`
- Modify: `Frontend/src/infraestructure/Dashboard.tsx`

**Interfaces:**
- Consumes: `GastoPublicidad` component (Task 9).
- Produces: the screen is reachable at `dashboard/publicidad` and shows up in the sidebar menu for any user who already has the "Gastos" module (`code: "1300"`).

- [ ] **Step 1: Add the menu entry**

In `Frontend/src/infraestructure/MData/MData.ts`, right after the "Gastos" entry (`id: 13`) and before the "Configuraciones" entry (`id: 14`), add:

```ts
  {
    // Comparte el code de Gastos a propósito (mismo patrón que "Configuraciones"
    // más abajo): evita crear un módulo/submódulo nuevo en AspNetModule/
    // AspNetSubModule y reasignar permisos por tenant solo para esta pantalla.
    code: "1300",
    id: 15,
    value: "ROI Publicidad",
    icon: "mdi:chart-line",
    url: "dashboard/publicidad",
  },
```

- [ ] **Step 2: Add the import and route**

In `Frontend/src/infraestructure/Dashboard.tsx`, add the import right after the `Gastos` import (line 25):

```tsx
import { GastoPublicidad } from "../presentation/views/Modules/Admin/Views/GastoPublicidad";
```

Add the route right after the `gastos` route (line 61):

```tsx
            <Route path="publicidad" element={<GastoPublicidad />}/>
```

- [ ] **Step 3: Verify it builds**

Run: `npm run build` (from `Frontend/`)
Expected: build succeeds with no TypeScript errors.

- [ ] **Step 4: Manual verification**

Run the frontend dev server (`npm run dev`) and the backend (`dotnet run --project WEB_API`), log in, confirm "ROI Publicidad" appears in the sidebar (for a user with access to "Gastos"), click into it, upload a real Meta Ads Excel export, assign each row to a product, confirm the import, and check the ROI table updates. Compare the numbers shown against a manual calculation in a separate spreadsheet for at least one product.

- [ ] **Step 5: Commit**

```bash
git add src/infraestructure/MData/MData.ts src/infraestructure/Dashboard.tsx
git commit -m "feat: add ROI Publicidad menu entry and route"
```
