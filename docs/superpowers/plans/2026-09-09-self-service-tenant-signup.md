# Self-Service Tenant Signup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let any business (of any `Rubro`) register itself in PuntoVenta through a public form, without a SuperAdmin, and become fully operational the moment it confirms its email — no config edit, no server restart, no manual account creation.

**Architecture:** Fix `TenantRegistry` to read active tenants from the database instead of a config file loaded once at startup (the actual blocker today). Extend the already-existing `CreateEmpresa`/`CreateUserAdmin` flow (it already auto-creates an admin user on every empresa creation) to accept real credentials and `EmailConfirmed = false` for the public path, while leaving the existing SuperAdmin-only path's behavior unchanged. Add email sending (Resend, plain HTTP) to deliver the confirmation link, reusing ASP.NET Identity's built-in confirmation-token machinery. The existing login (`AuthenticationRepository.Token`) already rejects unconfirmed users and disabled tenants — no changes needed there.

**Tech Stack:** .NET 6, EF Core (Npgsql in production, SQLite in-memory for tests via `TestDbContextFactory`), ASP.NET Identity (`UserManager<User>`), xUnit, React + Vite + Redux (plain `createReducer`/thunks, not RTK slices) for the frontend.

**Spec:** `docs/superpowers/specs/2026-09-09-self-service-tenant-signup-design.md`

## Global Constraints

- Do not change the behavior of the existing SuperAdmin-only endpoints (`crear-tenant`, `crear-empresa`, `add-empresa`) for their current callers — every new parameter added to shared methods must have a default that reproduces today's exact behavior.
- `Tenant` and `Empresa` (and `EmpresaTenant`) have **no** `HasQueryFilter` in `SpaContext.OnModelCreating` (confirmed by reading `Infrastructure/Data/SpaContext.cs`) — they are the global catalog that defines tenants, so code touching them never needs `IgnoreQueryFilters()` for tenant scoping (only for the currently-nonexistent-tenant bootstrap case, see Task 1).
- `AuthenticationRepository.Token()` already rejects login when `user.EmailConfirmed == false` (message: "Por favor, verifique su cuenta...") and when `user.Tenant.Activo == false` — do not duplicate these checks anywhere else.
- Follow existing patterns exactly: `(ServiceStatus, T, string)` tuples from repositories, `MessageResult<T>` from services, `ErrorHandler` exceptions from controllers are NOT used here (controllers just do `Ok(await service.X())`, matching `TenantController`'s existing style).
- Test style: SQLite in-memory via `TestDbContextFactory.CreateContext()` for anything touching `SpaContext`/Identity, exactly like `RoleRepositoryTests.cs`. Never use the EF InMemory provider for anything involving a transaction.
- Frontend: no RTK `createSlice` — this codebase's `redux/reducers/*` use plain `createReducer` + string action types + thunks dispatching plain `{ type, payload }` objects, calling `axiosInstance` from `utils/axios.ts`. Match this exactly, do not introduce a different pattern.

---

## Task 1: `TenantRegistry` reads active tenants from the database

**Files:**
- Modify: `Infrastructure/TenantRegistry.cs`
- Modify: `WEB_API/Program.cs:136` (DI lifetime)
- Test: `Tests/Infrastructure.Tests/TenantRegistryTests.cs` (new file)

**Interfaces:**
- Produces: `ITenantRegistry.GetTenants()` — same signature as today, now backed by a live DB query instead of `appsettings.json`. `TenantResolver.cs` (consumer) needs zero changes.
- Produces (internal, testable): `TenantRegistry.GetActiveTenants(SpaContext context): Tenantx[]` — the query logic, factored out so it's testable against the SQLite in-memory `SpaContext` without a real Postgres connection.

- [ ] **Step 1: Write the failing test**

```csharp
// Tests/Infrastructure.Tests/TenantRegistryTests.cs
using Domain.Entities;
using Xunit;

namespace Infrastructure.Tests;

public class TenantRegistryTests
{
    [Fact]
    public async Task GetActiveTenants_DevuelveSoloTenantsActivos()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var rubro = new Domain.Entities.Rubro { Nombre = "Barbería" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        context.Tenant.Add(new Tenant { Identificador = 1, Name = "ACTIVO1", TenantKey = "activo", RubroId = rubro.Id, Activo = true });
        context.Tenant.Add(new Tenant { Identificador = 2, Name = "INACTIVO1", TenantKey = "inactivo", RubroId = rubro.Id, Activo = false });
        await context.SaveChangesAsync();

        var result = TenantRegistry.GetActiveTenants(context);

        Assert.Single(result);
        Assert.Equal("ACTIVO1", result[0].Name);
        Assert.Equal("activo", result[0].TenantKey);
    }

    [Fact]
    public async Task GetActiveTenants_SinTenants_DevuelveArregloVacio()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var result = TenantRegistry.GetActiveTenants(context);

        Assert.Empty(result);
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter TenantRegistryTests`
Expected: FAIL to compile — `TenantRegistry.GetActiveTenants` doesn't exist yet.

- [ ] **Step 3: Write the implementation**

```csharp
// Infrastructure/TenantRegistry.cs
using Application.Abstractions;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class TenantRegistry : ITenantRegistry
{
    private readonly string _defaultConnection;

    public TenantRegistry(IConfiguration configuration)
    {
        _defaultConnection = configuration.GetSection("TenantOptions").GetValue<string>("DefaultConnection")
            ?? configuration.GetValue<string>("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró TenantOptions:DefaultConnection en la configuración.");
    }

    public Tenantx[] GetTenants()
    {
        var options = new DbContextOptionsBuilder<SpaContext>().UseNpgsql(_defaultConnection).Options;
        var bootstrap = new Tenantx { Name = "public", ConnectionString = _defaultConnection };
        using var context = new SpaContext(options, new BootstrapTenantResolver(bootstrap));
        return GetActiveTenants(context);
    }

    /// <summary>
    /// Query logic factored out from GetTenants() so it can be unit-tested against
    /// the SQLite in-memory SpaContext (TestDbContextFactory) without a real Postgres
    /// connection. Tenant/Empresa carry no HasQueryFilter (see SpaContext.OnModelCreating),
    /// so this reads every tenant regardless of which tenant "context" was bootstrapped with.
    /// </summary>
    internal static Tenantx[] GetActiveTenants(SpaContext context)
    {
        return context.Tenant
            .Include(t => t.Moneda)
            .Where(t => t.Activo)
            .Select(t => new Tenantx
            {
                Name = t.Name,
                TenantKey = t.TenantKey,
                RubroId = t.RubroId,
                PaisId = t.PaisId,
                MonedaCodigo = t.Moneda != null ? t.Moneda.Codigo : null,
            })
            .ToArray();
    }

    private class BootstrapTenantResolver : ITenantResolver
    {
        private readonly Tenantx _tenant;
        public BootstrapTenantResolver(Tenantx tenant) => _tenant = tenant;
        public Tenantx GetCurrentTenant() => _tenant;
    }
}
```

This mirrors the exact bootstrap idiom already used in `Infrastructure/Data/MigrationDbContextFactory.cs` (a throwaway `ITenantResolver` implementation feeding a `Tenantx{Name="public", ConnectionString=...}` into `SpaContext`'s constructor) — not a new pattern.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter TenantRegistryTests`
Expected: PASS.

- [ ] **Step 5: Change the DI lifetime**

In `WEB_API/Program.cs`, change line 136 from:

```csharp
builder.Services.AddSingleton<ITenantRegistry, TenantRegistry>();
```

to:

```csharp
builder.Services.AddScoped<ITenantRegistry, TenantRegistry>();
```

`AddSingleton` is exactly why a newly-created tenant is invisible until a restart today — it built the tenant list once, at app startup. `AddScoped` re-runs `GetTenants()` (one Postgres query) per request; the plan's own spec (section "Riesgos") already flags confirming this has no perceptible latency impact as a follow-up, not a blocker — tenant counts here are small.

- [ ] **Step 6: Run the full backend test suite**

Run: `dotnet test Backend`
Expected: all tests PASS, no regressions (this change only affects how `ITenantRegistry` is implemented and registered — every other consumer of `ITenantRegistry`/`TenantResolver` is untouched).

- [ ] **Step 7: Commit**

```bash
git add Backend/Infrastructure/TenantRegistry.cs Backend/WEB_API/Program.cs Backend/Tests/Infrastructure.Tests/TenantRegistryTests.cs
git commit -m "fix: resolve tenants from the database instead of static config

A tenant created via POST /api/tenant/crear-tenant was invisible to
TenantResolver until the process restarted, because TenantRegistry read
appsettings.json's TenantOptions:Tenants as an AddSingleton loaded once
at startup. It now queries the Tenant table (Activo=true) per request.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

## Task 2: `IEmailService` backed by Resend

**Files:**
- Create: `Application/Interfaces/IEmailService.cs`
- Create: `Infrastructure/Services/EmailService.cs`
- Modify: `WEB_API/Program.cs` (DI registration)
- Test: `Tests/Infrastructure.Tests/EmailServiceTests.cs` (new file)

**Interfaces:**
- Produces: `IEmailService.EnviarAsync(string destinatario, string asunto, string htmlBody): Task` — consumed by Task 3's `RegistroPublico`.
- Requires config: `RESEND_API_KEY`, `RESEND_FROM_EMAIL` (read via `IConfiguration`, same style as other settings in this project — add to `appsettings.json`'s structure as a new `Resend` section: `Resend:ApiKey`, `Resend:FromEmail`).

- [ ] **Step 1: Write the interface**

```csharp
// Application/Interfaces/IEmailService.cs
namespace Application.Interfaces;

public interface IEmailService
{
    Task EnviarAsync(string destinatario, string asunto, string htmlBody);
}
```

- [ ] **Step 2: Write the failing test**

```csharp
// Tests/Infrastructure.Tests/EmailServiceTests.cs
using System.Net;
using System.Text.Json;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace Infrastructure.Tests;

public class EmailServiceTests
{
    private static (EmailService service, Mock<HttpMessageHandler> handler) Build(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("{}") });

        var httpClient = new HttpClient(handler.Object);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resend:ApiKey"] = "test-key",
                ["Resend:FromEmail"] = "notificaciones@puntoventa.test",
            })
            .Build();

        return (new EmailService(httpClient, config), handler);
    }

    [Fact]
    public async Task EnviarAsync_MandaElBearerYElBodyCorrectos()
    {
        var (service, handler) = Build();

        await service.EnviarAsync("dueno@negocio.test", "Confirma tu cuenta", "<p>Hola</p>");

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString() == "https://api.resend.com/emails" &&
                req.Headers.Authorization!.ToString() == "Bearer test-key"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task EnviarAsync_LanzaExcepcionSiResendRechazaElEnvio()
    {
        var (service, _) = Build(HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnviarAsync("dueno@negocio.test", "Confirma tu cuenta", "<p>Hola</p>"));
    }
}
```

This test needs the `Moq` package (for mocking `HttpMessageHandler`). Check `Tests/Infrastructure.Tests/Infrastructure.Tests.csproj` first — if `Moq` isn't already a package reference there, add it (`dotnet add Backend/Tests/Infrastructure.Tests package Moq`) before writing the test; do not silently skip the test if it's missing.

- [ ] **Step 3: Run it to verify it fails**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter EmailServiceTests`
Expected: FAIL to compile — `Infrastructure.Services.EmailService` doesn't exist yet.

- [ ] **Step 4: Write the implementation**

```csharp
// Infrastructure/Services/EmailService.cs
using System.Net.Http.Json;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _fromEmail;

    public EmailService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Resend:ApiKey"] ?? throw new InvalidOperationException("Falta configurar Resend:ApiKey.");
        _fromEmail = configuration["Resend:FromEmail"] ?? throw new InvalidOperationException("Falta configurar Resend:FromEmail.");
    }

    public async Task EnviarAsync(string destinatario, string asunto, string htmlBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new { from = _fromEmail, to = new[] { destinatario }, subject = asunto, html = htmlBody });

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend rechazó el envío ({(int)response.StatusCode}): {body}");
        }
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter EmailServiceTests`
Expected: PASS.

- [ ] **Step 6: Register the service and its `HttpClient`**

In `WEB_API/Program.cs`, near the other `builder.Services.Add...` registrations (e.g. next to line 136), add:

```csharp
builder.Services.AddHttpClient<IEmailService, Infrastructure.Services.EmailService>();
```

- [ ] **Step 7: Document the new config keys**

In `WEB_API/appsettings.json` (and any environment-specific copies, e.g. `appsettings.Development.json` if one exists — check first), add a new top-level section (values left empty/placeholder for local dev, real values set via environment variables or secrets in each deployment, matching how `TenantOptions:DefaultConnection` is already handled):

```json
"Resend": {
  "ApiKey": "",
  "FromEmail": ""
}
```

- [ ] **Step 8: Run the full backend test suite**

Run: `dotnet test Backend`
Expected: all PASS.

- [ ] **Step 9: Commit**

```bash
git add Backend/Application/Interfaces/IEmailService.cs Backend/Infrastructure/Services/EmailService.cs Backend/WEB_API/Program.cs Backend/WEB_API/appsettings.json Backend/Tests/Infrastructure.Tests/EmailServiceTests.cs Backend/Tests/Infrastructure.Tests/Infrastructure.Tests.csproj
git commit -m "feat: add IEmailService backed by Resend's HTTP API

No email-sending capability existed anywhere in the backend. This is a
plain HttpClient POST to api.resend.com — no SDK — needed by the
upcoming public tenant signup flow to deliver confirmation links.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

## Task 3: Public signup — real admin credentials, unconfirmed until verified

**Files:**
- Modify: `Infrastructure/Repositories/TenantRepository.cs` (extend `CreateUserAdmin`, extend `CreateEmpresa`, add `RegistroPublico`)
- Modify: `Application/Interfaces/IRepository/ITenantRepository.cs` (extend `CreateEmpresa` signature, add `RegistroPublico`)
- Modify: `Application/Services/TenantService.cs` (add `RegistroPublico`)
- Modify: `Application/Interfaces/IServices/ITenantService.cs` (add `RegistroPublico`)
- Create: `Domain/Payloads/RegistroPublicoPayload.cs`
- Modify: `WEB_API/Controllers/TenantController.cs` (new `[AllowAnonymous]` endpoint)
- Test: `Tests/Infrastructure.Tests/TenantRepositoryTests.cs` (new file)

**Interfaces:**
- Consumes: `IEmailService.EnviarAsync` from Task 2.
- Produces: `ITenantRepository.RegistroPublico(RegistroPublicoPayload): Task<(ServiceStatus, object?, string)>` where the `object?` on success is `new { userId, tenantName }` — consumed by Task 4's confirmation flow only indirectly (Task 4 looks the user up by the link's own `userId`/`token` query params, not by calling back into this task's code).
- The existing `CreateEmpresa(CreateEmpresaPayload)` interface signature gains three optional parameters. Every existing call site (`TenantService.CreateEmpresa`, and transitively the SuperAdmin-only `POST /api/tenant/crear-empresa`) keeps compiling and behaving identically because it never passes them.

- [ ] **Step 1: Write the new payload**

```csharp
// Domain/Payloads/RegistroPublicoPayload.cs
namespace Domain.Payloads;

public class RegistroPublicoPayload
{
    public string NombreNegocio { get; set; } = null!;
    public int RubroId { get; set; }
    public string AdminEmail { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Ruc { get; set; }
    public string? NombreComercial { get; set; }
    public string? RazonSocial { get; set; }
    public string? Direccion { get; set; }
    public string? Celular { get; set; }
    public string? UbigeoId { get; set; }
}
```

- [ ] **Step 2: Write the failing test**

```csharp
// Tests/Infrastructure.Tests/TenantRepositoryTests.cs
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Identity;
using Domain.Payloads;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Infrastructure.Tests;

public class TenantRepositoryTests
{
    private static (TenantRepository repo, UserManager<User> userManager, Data.SpaContext context) Build()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        // Connection intentionally not disposed here per-test; callers own it via TestDbContextFactory's tuple like other tests in this file's neighbors.
        var store = new UserStore<User, Role, Data.SpaContext, string>(context);
        var userManager = new UserManager<User>(store, null!, new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!,
            new NullLogger<UserManager<User>>());

        var repo = new TenantRepository(context, TestDbContextFactory.Mapper, userManager);
        return (repo, userManager, context);
    }

    [Fact]
    public async Task RegistroPublico_CreaTenantEmpresaYUsuarioSinConfirmar_YEnviaCorreo()
    {
        var (repo, userManager, context) = Build();
        var rubro = new Rubro { Nombre = "Barbería" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var emailService = new Mock<Application.Interfaces.IEmailService>();

        var payload = new RegistroPublicoPayload
        {
            NombreNegocio = "Barbería El Corte",
            RubroId = rubro.Id,
            AdminEmail = "dueno@barberia.test",
            AdminPassword = "ClaveSegura123!",
        };

        var (estado, data, mensaje) = await repo.RegistroPublico(payload, emailService.Object);

        Assert.Equal(ServiceStatus.Ok, estado);

        var tenant = await context.Tenant.FirstAsync(t => t.TenantKey == "barbería el corte" || t.TenantKey == "barberia el corte" || t.RubroId == rubro.Id);
        Assert.True(tenant.Activo);

        var user = await userManager.FindByNameAsync(tenant.Name);
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.Equal("dueno@barberia.test", user.Email);

        emailService.Verify(e => e.EnviarAsync("dueno@barberia.test", It.IsAny<string>(), It.Is<string>(html => html.Contains(user.Id))), Times.Once);
    }
}
```

**Note on the test above:** `CreateTenant`'s existing normalization (`nombre.Trim().ToLower()`) means the exact `TenantKey` string depends on how `CreateTenant` lower-cases the name — read `TenantRepository.CreateTenant` (already unchanged, at the top of this same file) before finalizing this assertion, and assert against whatever the actual lower-cased value is rather than guessing between `barbería`/`barberia`. Adjust the `Assert.Equal`/`FirstAsync` predicate to match reality once you see the first test run's failure output — this is expected finishing work, not a sign the approach is wrong.

- [ ] **Step 3: Run it to verify it fails**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter TenantRepositoryTests`
Expected: FAIL to compile — `RegistroPublico` doesn't exist on `TenantRepository` yet.

- [ ] **Step 4: Extend `CreateUserAdmin`**

In `Infrastructure/Repositories/TenantRepository.cs`, change:

```csharp
    private async Task<User> CreateUserAdmin(string username, int identificador)
    {
        var newUser = new User
        {
            FirstName = username,
            LastName = username,
            FechaCreacion = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss"),
            UserName = username,
            TenantId = identificador,
            Estado = true,
            Email = "",
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(newUser, "123456");

        if (!result.Succeeded)
            throw new Exception($"No se pudo crear el usuario admin \"{username}\": {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return newUser;
    }
```

to:

```csharp
    private async Task<User> CreateUserAdmin(string username, int identificador, string email = "", string password = "123456", bool emailConfirmed = true)
    {
        var newUser = new User
        {
            FirstName = username,
            LastName = username,
            FechaCreacion = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss"),
            UserName = username,
            TenantId = identificador,
            Estado = true,
            Email = email,
            EmailConfirmed = emailConfirmed,
        };

        var result = await _userManager.CreateAsync(newUser, password);

        if (!result.Succeeded)
            throw new Exception($"No se pudo crear el usuario admin \"{username}\": {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return newUser;
    }
```

The two existing call sites (inside `CreateEmpresa` and `AddEmpresaTenant`) call this with only 2 positional args — they keep compiling and behave identically (blank email, password `"123456"`, `EmailConfirmed = true`, exactly as today).

- [ ] **Step 5: Extend `CreateEmpresa`**

In the same file, change the `CreateEmpresa` signature and its internal call from:

```csharp
    public async Task<(ServiceStatus, object?, string)> CreateEmpresa(CreateEmpresaPayload payload)
    {
        try
        {
            //TODO: HACER CON TRANSACCION
            var entity = mapper.Map<Empresa>(payload);
```

to:

```csharp
    public async Task<(ServiceStatus, object?, string)> CreateEmpresa(CreateEmpresaPayload payload, string? adminEmail = null, string? adminPassword = null, bool adminEmailConfirmed = true)
    {
        try
        {
            //TODO: HACER CON TRANSACCION
            var entity = mapper.Map<Empresa>(payload);
```

and further down in the same method, change:

```csharp
            //Crear Usuario
            var userResult = await CreateUserAdmin(userAdmin, payload.TenantId);
```

to:

```csharp
            //Crear Usuario
            var userResult = await CreateUserAdmin(userAdmin, payload.TenantId, adminEmail ?? "", adminPassword ?? "123456", adminEmailConfirmed);
```

`AddEmpresaTenant` is a separate method with its own `CreateUserAdmin` call — leave it completely untouched.

- [ ] **Step 6: Add `RegistroPublico`**

In the same file, add this new public method (near `CreateEmpresa`, since it orchestrates the same pieces):

```csharp
    public async Task<(ServiceStatus, object?, string)> RegistroPublico(RegistroPublicoPayload payload, IEmailService emailService)
    {
        try
        {
            var (estadoTenant, tenantId, mensajeTenant) = await CreateTenant(payload.NombreNegocio, payload.RubroId, null);
            if (estadoTenant != ServiceStatus.Ok)
                return (estadoTenant, null, mensajeTenant);

            var tenant = await dbContext.Tenant.AsNoTracking().FirstAsync(t => t.Identificador == tenantId);

            var empresaPayload = new CreateEmpresaPayload
            {
                TenantId = tenantId,
                Email = payload.AdminEmail,
                Telefono = payload.Telefono,
                Ruc = payload.Ruc,
                NombreComercial = payload.NombreComercial ?? payload.NombreNegocio,
                RazonSocial = payload.RazonSocial,
                Direccion = payload.Direccion,
                Celular = payload.Celular,
                UbigeoId = payload.UbigeoId,
            };

            var (estadoEmpresa, _, mensajeEmpresa) = await CreateEmpresa(empresaPayload, payload.AdminEmail, payload.AdminPassword, adminEmailConfirmed: false);
            if (estadoEmpresa != ServiceStatus.Ok)
                return (estadoEmpresa, null, mensajeEmpresa);

            var user = await _userManager.FindByNameAsync(tenant.Name);
            if (user is null)
                return (ServiceStatus.InternalError, null, "El usuario admin no se encontró tras crearlo");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var enlace = $"{_baseUrlFrontend}/confirmar-registro?userId={user.Id}&token={encodedToken}";

            await emailService.EnviarAsync(payload.AdminEmail, "Confirma tu cuenta de PuntoVenta",
                $"<p>Hola,</p><p>Confirma tu cuenta para activar <strong>{payload.NombreNegocio}</strong> en PuntoVenta:</p><p><a href=\"{enlace}\">Confirmar mi cuenta</a></p><p>userId: {user.Id}</p>");

            return (ServiceStatus.Ok, new { userId = user.Id, tenantName = tenant.Name }, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error en registro público -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
```

This references `_baseUrlFrontend`, a new field for the confirmation link's base URL. Add it to the constructor:

```csharp
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;
    private readonly UserManager<User> _userManager;
    private readonly string _baseUrlFrontend;

    public TenantRepository(SpaContext dbContext, IMapper mapper, UserManager<User> userManager, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        _userManager = userManager;
        _baseUrlFrontend = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
    }
```

Add `"Frontend": { "BaseUrl": "" }` to `WEB_API/appsettings.json` alongside the `Resend` section from Task 2 (empty for local dev, set per environment).

**Note:** this constructor change adds a new required constructor parameter. `TenantRepository` is resolved via DI (`ITenantRepository` → `TenantRepository`), so no manual `new TenantRepository(...)` call sites exist in production code to update — only this plan's own tests construct it directly (Step 2 above already passes 3 args to match the *pre-change* constructor; **update that test's `Build()` helper to also pass a 4th `IConfiguration` argument** once you reach this step, e.g. `new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Frontend:BaseUrl"]="http://localhost:5173"}).Build()`).

- [ ] **Step 7: Update the two interfaces**

In `Application/Interfaces/IRepository/ITenantRepository.cs`, change:

```csharp
        Task<(ServiceStatus, object?, string)> CreateEmpresa(CreateEmpresaPayload payload);
```

to:

```csharp
        Task<(ServiceStatus, object?, string)> CreateEmpresa(CreateEmpresaPayload payload, string? adminEmail = null, string? adminPassword = null, bool adminEmailConfirmed = true);
        Task<(ServiceStatus, object?, string)> RegistroPublico(RegistroPublicoPayload payload, Application.Interfaces.IEmailService emailService);
```

In `Application/Interfaces/IServices/ITenantService.cs`, add:

```csharp
    Task<MessageResult<object>> RegistroPublico(RegistroPublicoPayload payload);
```

- [ ] **Step 8: Add the service method**

In `Application/Services/TenantService.cs`, add a method following the exact pattern of the existing `CreateEmpresa` service method in that file (read it first for the exact try/throw-`ErrorHandler` idiom used there), injecting `IEmailService` as a new constructor dependency:

```csharp
    public async Task<MessageResult<object>> RegistroPublico(RegistroPublicoPayload payload)
    {
        var (estado, result, message) = await tenantRepository.RegistroPublico(payload, emailService);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? System.Net.HttpStatusCode.BadRequest
                    : System.Net.HttpStatusCode.InternalServerError
                , "Error en registro público", message);

        return MessageResult<object>.Of(message, result);
    }
```

Add `private readonly IEmailService emailService;` and thread it through the constructor (read the existing constructor first and add the parameter alongside the existing `tenantRepository` injection).

- [ ] **Step 9: Add the controller endpoint**

In `WEB_API/Controllers/TenantController.cs`, add:

```csharp
    [AllowAnonymous]
    [HttpPost("registro-publico")]
    public async Task<IActionResult> RegistroPublico([FromBody] RegistroPublicoPayload payload) => Ok(await tenantService.RegistroPublico(payload));
```

- [ ] **Step 10: Run the test to verify it passes**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter TenantRepositoryTests`
Expected: PASS. If the `TenantKey` assertion from Step 2 doesn't match, fix the test's expected string to match `CreateTenant`'s actual normalization — do not change `CreateTenant` itself.

- [ ] **Step 11: Run the full backend test suite**

Run: `dotnet test Backend`
Expected: all PASS — in particular, re-check any existing test that calls `CreateEmpresa` or constructs `TenantRepository` directly still compiles against the new optional parameters and the new constructor parameter.

- [ ] **Step 12: Commit**

```bash
git add Backend/Domain/Payloads/RegistroPublicoPayload.cs Backend/Infrastructure/Repositories/TenantRepository.cs Backend/Application/Interfaces/IRepository/ITenantRepository.cs Backend/Application/Services/TenantService.cs Backend/Application/Interfaces/IServices/ITenantService.cs Backend/WEB_API/Controllers/TenantController.cs Backend/WEB_API/appsettings.json Backend/Tests/Infrastructure.Tests/TenantRepositoryTests.cs
git commit -m "feat: public self-service tenant signup with unconfirmed admin user

Reuses the existing CreateTenant/CreateEmpresa/CreateUserAdmin flow that
already auto-creates an admin user on every empresa. Extends it with
optional real-credential parameters (default values reproduce today's
SuperAdmin-flow behavior exactly) and sends a confirmation email via the
new IEmailService. Login already rejects unconfirmed users — no change
needed there.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

## Task 4: Email confirmation endpoint

**Files:**
- Modify: `Infrastructure/Repositories/TenantRepository.cs` (`ConfirmarRegistro`)
- Modify: `Application/Interfaces/IRepository/ITenantRepository.cs`
- Modify: `Application/Services/TenantService.cs`
- Modify: `Application/Interfaces/IServices/ITenantService.cs`
- Modify: `WEB_API/Controllers/TenantController.cs`
- Test: extend `Tests/Infrastructure.Tests/TenantRepositoryTests.cs`

**Interfaces:**
- Produces: `ITenantRepository.ConfirmarRegistro(string userId, string token): Task<(ServiceStatus, string)>` — no other task consumes this; it's the terminal step of the signup flow.

- [ ] **Step 1: Write the failing test**

Add to `Tests/Infrastructure.Tests/TenantRepositoryTests.cs`:

```csharp
    [Fact]
    public async Task ConfirmarRegistro_ConTokenValido_ActivaElCorreo()
    {
        var (repo, userManager, context) = Build();
        var rubro = new Rubro { Nombre = "Barbería" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var emailService = new Mock<Application.Interfaces.IEmailService>();
        var payload = new RegistroPublicoPayload { NombreNegocio = "Barbería El Corte", RubroId = rubro.Id, AdminEmail = "dueno@barberia.test", AdminPassword = "ClaveSegura123!" };
        var (_, data, _) = await repo.RegistroPublico(payload, emailService.Object);
        dynamic registroData = data!;
        string userId = registroData.userId;

        var user = await userManager.FindByIdAsync(userId);
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);

        var (estado, mensaje) = await repo.ConfirmarRegistro(userId, token);

        Assert.Equal(ServiceStatus.Ok, estado);
        var confirmado = await userManager.FindByIdAsync(userId);
        Assert.True(confirmado!.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmarRegistro_ConTokenInvalido_Falla()
    {
        var (repo, userManager, context) = Build();
        var rubro = new Rubro { Nombre = "Barbería" };
        context.Rubro.Add(rubro);
        await context.SaveChangesAsync();

        var emailService = new Mock<Application.Interfaces.IEmailService>();
        var payload = new RegistroPublicoPayload { NombreNegocio = "Barbería El Corte", RubroId = rubro.Id, AdminEmail = "dueno@barberia.test", AdminPassword = "ClaveSegura123!" };
        var (_, data, _) = await repo.RegistroPublico(payload, emailService.Object);
        dynamic registroData = data!;
        string userId = registroData.userId;

        var (estado, mensaje) = await repo.ConfirmarRegistro(userId, "token-invalido");

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        var noConfirmado = await userManager.FindByIdAsync(userId);
        Assert.False(noConfirmado!.EmailConfirmed);
    }
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter TenantRepositoryTests`
Expected: FAIL to compile — `ConfirmarRegistro` doesn't exist yet.

- [ ] **Step 3: Write the implementation**

In `Infrastructure/Repositories/TenantRepository.cs`, add:

```csharp
    public async Task<(ServiceStatus, string)> ConfirmarRegistro(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return (ServiceStatus.FailedValidation, "El usuario no existe");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                return (ServiceStatus.FailedValidation, string.Join("; ", result.Errors.Select(e => e.Description)));

            return (ServiceStatus.Ok, "Cuenta confirmada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, $"Error al confirmar registro -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
```

- [ ] **Step 4: Update interfaces, service, and controller**

`ITenantRepository.cs`, add: `Task<(ServiceStatus, string)> ConfirmarRegistro(string userId, string token);`

`ITenantService.cs`, add: `Task<MessageResult<string>> ConfirmarRegistro(string userId, string token);`

`TenantService.cs`, add (same try/throw pattern as `RegistroPublico` above, adjusted for the 2-tuple return):

```csharp
    public async Task<MessageResult<string>> ConfirmarRegistro(string userId, string token)
    {
        var (estado, mensaje) = await tenantRepository.ConfirmarRegistro(userId, token);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(System.Net.HttpStatusCode.BadRequest, "Error al confirmar registro", mensaje);

        return MessageResult<string>.Of(mensaje, mensaje);
    }
```

`TenantController.cs`, add:

```csharp
    [AllowAnonymous]
    [HttpGet("confirmar-registro")]
    public async Task<IActionResult> ConfirmarRegistro([FromQuery] string userId, [FromQuery] string token) => Ok(await tenantService.ConfirmarRegistro(userId, token));
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter TenantRepositoryTests`
Expected: PASS.

- [ ] **Step 6: Run the full backend test suite**

Run: `dotnet test Backend`
Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git add Backend/Infrastructure/Repositories/TenantRepository.cs Backend/Application/Interfaces/IRepository/ITenantRepository.cs Backend/Application/Services/TenantService.cs Backend/Application/Interfaces/IServices/ITenantService.cs Backend/WEB_API/Controllers/TenantController.cs Backend/Tests/Infrastructure.Tests/TenantRepositoryTests.cs
git commit -m "feat: add email confirmation endpoint for public tenant signup

Uses ASP.NET Identity's built-in ConfirmEmailAsync - no custom token
logic. Once confirmed, the existing login check (EmailConfirmed) lets
the new admin user in immediately.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

## Task 5: Public signup page (Frontend)

**Files:**
- Create: `Frontend/src/presentation/views/Registro/index.tsx`
- Create: `Frontend/src/presentation/views/Registro/registro.module.css`
- Create: `Frontend/src/redux/reducers/registro/registro.reducer.ts`
- Create: `Frontend/src/redux/reducers/registro/interfaces/index.ts`
- Create: `Frontend/src/redux/reducers/registro/types/index.ts`
- Modify: `Frontend/src/infraestructure/Dashboard.tsx` (register the route)
- Modify: `Frontend/src/redux/rootState.ts` or wherever reducers are combined (read it first — mirror how `authReducer` is wired in)

**Interfaces:**
- Consumes: `POST /api/tenant/registro-publico` (Task 3), `GET /api/tenant/confirmar-registro` (Task 4), `GET /api/extensiones/rubros` (already exists, used for the Rubro dropdown).
- No automated test — this codebase's frontend has no test setup (confirmed: no `.test.tsx`/`.spec.tsx` files exist anywhere under `Frontend/src`, and no test runner is configured in `Frontend/package.json`). Verify manually per Step 6 below instead.

- [ ] **Step 1: Find where reducers are combined**

Read `Frontend/src/redux/store.ts` and `Frontend/src/redux/rootState.ts` (or equivalent — the exact filenames may differ slightly; use the import `../../redux/rootState` and `../../redux/store` from `Login/index.tsx` as your starting point) to see exactly how `authReducer` is registered, so the new `registroReducer` is wired in identically.

- [ ] **Step 2: Write the redux types and interfaces**

```ts
// Frontend/src/redux/reducers/registro/types/index.ts
export const REGISTRO_LOADING = "REGISTRO_LOADING";
export const REGISTRO_SUCCESS = "REGISTRO_SUCCESS";
export const REGISTRO_ERROR = "REGISTRO_ERROR";
export const CONFIRMAR_LOADING = "CONFIRMAR_LOADING";
export const CONFIRMAR_SUCCESS = "CONFIRMAR_SUCCESS";
export const CONFIRMAR_ERROR = "CONFIRMAR_ERROR";

export interface IRegistroSuccess {
  type: typeof REGISTRO_SUCCESS;
  payload: { userId: string; tenantName: string };
}
export interface IRegistroError {
  type: typeof REGISTRO_ERROR;
  payload: string;
}
export interface IConfirmarSuccess {
  type: typeof CONFIRMAR_SUCCESS;
  payload: string;
}
export interface IConfirmarError {
  type: typeof CONFIRMAR_ERROR;
  payload: string;
}
```

```ts
// Frontend/src/redux/reducers/registro/interfaces/index.ts
export interface IRegistroState {
  loading: boolean;
  registrado: boolean;
  confirmado: boolean;
  message: string;
}

export interface IRegistroPayload {
  nombreNegocio: string;
  rubroId: number;
  adminEmail: string;
  adminPassword: string;
  telefono?: string;
  ruc?: string;
  direccion?: string;
}
```

- [ ] **Step 3: Write the reducer + thunks**

```ts
// Frontend/src/redux/reducers/registro/registro.reducer.ts
import { createReducer } from "@reduxjs/toolkit";
import { Dispatch, AnyAction } from "redux";
import axiosInstance from "../../../utils/axios";
import { IRegistroState, IRegistroPayload } from "./interfaces";
import * as types from "./types";

const initialState: IRegistroState = {
  loading: false,
  registrado: false,
  confirmado: false,
  message: "",
};

export const registroReducer = createReducer(initialState, (builder) => {
  builder
    .addCase(types.REGISTRO_LOADING, (state): IRegistroState => ({ ...state, loading: true, message: "" }))
    .addCase(types.REGISTRO_SUCCESS, (state): IRegistroState => ({ ...state, loading: false, registrado: true }))
    .addCase(types.REGISTRO_ERROR, (state, action: types.IRegistroError): IRegistroState => ({ ...state, loading: false, message: action.payload }))
    .addCase(types.CONFIRMAR_LOADING, (state): IRegistroState => ({ ...state, loading: true, message: "" }))
    .addCase(types.CONFIRMAR_SUCCESS, (state): IRegistroState => ({ ...state, loading: false, confirmado: true }))
    .addCase(types.CONFIRMAR_ERROR, (state, action: types.IConfirmarError): IRegistroState => ({ ...state, loading: false, message: action.payload }));
});

export const registrarNegocio = (payload: IRegistroPayload) => {
  return async (dispatch: Dispatch<AnyAction>) => {
    dispatch({ type: types.REGISTRO_LOADING });
    try {
      const { data } = await axiosInstance.post(`/tenant/registro-publico`, {
        NombreNegocio: payload.nombreNegocio,
        RubroId: payload.rubroId,
        AdminEmail: payload.adminEmail,
        AdminPassword: payload.adminPassword,
        Telefono: payload.telefono,
        Ruc: payload.ruc,
        Direccion: payload.direccion,
      });
      dispatch({ type: types.REGISTRO_SUCCESS, payload: data.data });
    } catch (error: any) {
      dispatch({ type: types.REGISTRO_ERROR, payload: error?.response?.data?.message ?? "No se pudo completar el registro" });
    }
  };
};

export const confirmarRegistro = (userId: string, token: string) => {
  return async (dispatch: Dispatch<AnyAction>) => {
    dispatch({ type: types.CONFIRMAR_LOADING });
    try {
      const { data } = await axiosInstance.get(`/tenant/confirmar-registro`, { params: { userId, token } });
      dispatch({ type: types.CONFIRMAR_SUCCESS, payload: data.message });
    } catch (error: any) {
      dispatch({ type: types.CONFIRMAR_ERROR, payload: error?.response?.data?.message ?? "No se pudo confirmar tu cuenta" });
    }
  };
};

export default registroReducer;
```

- [ ] **Step 4: Wire the reducer into the store**

Follow exactly what Step 1 found for `authReducer` — add `registro: registroReducer` (or whatever key naming convention that file uses) to the combined reducers object, and add `IRegistroState` to `RootState` if it's an explicitly typed interface there.

- [ ] **Step 5: Write the signup view**

```tsx
// Frontend/src/presentation/views/Registro/index.tsx
import styles from "./registro.module.css";
import { ChangeEvent, useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../../../redux/store";
import { RootState } from "../../../redux/rootState";
import { Toaster, toast } from "sonner";
import Input from "../../../components/Input";
import { registrarNegocio } from "../../../redux/reducers/registro/registro.reducer";
import { IRegistroPayload } from "../../../redux/reducers/registro/interfaces";
import axiosInstance from "../../../utils/axios";

interface IRubro {
  id: number;
  nombre: string;
}

const initialForm: IRegistroPayload = {
  nombreNegocio: "",
  rubroId: 0,
  adminEmail: "",
  adminPassword: "",
};

const Registro = () => {
  const dispatch = useAppDispatch();
  const { loading, registrado, message } = useAppSelector((state: RootState) => (state as any).registro);
  const [formValues, setFormValues] = useState<IRegistroPayload>(initialForm);
  const [rubros, setRubros] = useState<IRubro[]>([]);

  useEffect(() => {
    axiosInstance.get(`/extensiones/rubros`).then(({ data }) => setRubros(data.data ?? [])).catch(() => setRubros([]));
  }, []);

  useEffect(() => {
    if (message) toast.error(message);
  }, [message]);

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormValues((prev) => ({ ...prev, [name]: name === "rubroId" ? Number(value) : value }));
  };

  const handleSubmit = () => {
    if (!formValues.nombreNegocio || !formValues.rubroId || !formValues.adminEmail || !formValues.adminPassword) {
      toast.error("Completa nombre del negocio, rubro, correo y contraseña");
      return;
    }
    dispatch(registrarNegocio(formValues));
  };

  if (registrado) {
    return (
      <div className={styles.registro__wrapper}>
        <div className={styles.registro__form}>
          <h5>¡Listo! Revisa tu correo</h5>
          <p>Te enviamos un enlace a {formValues.adminEmail} para activar tu cuenta.</p>
        </div>
      </div>
    );
  }

  return (
    <>
      {message && <Toaster position="top-right" richColors />}
      <div className={styles.registro__wrapper}>
        <div className={styles.registro__form}>
          <div className={styles.header__registroInfo}>
            <h5>Registra tu negocio</h5>
            <p>Crea tu cuenta en PuntoVenta, sin necesidad de que nadie te la active.</p>
          </div>
          <div className={styles.form__wrapper}>
            <Input isLabel label="Nombre del negocio" name="nombreNegocio" onChange={handleChange} type="text" />
            <div>
              <label>Rubro</label>
              <select name="rubroId" onChange={handleChange} value={formValues.rubroId}>
                <option value={0}>Selecciona un rubro</option>
                {rubros.map((r) => (
                  <option key={r.id} value={r.id}>{r.nombre}</option>
                ))}
              </select>
            </div>
            <Input isLabel label="Correo del administrador" name="adminEmail" onChange={handleChange} type="email" />
            <Input isLabel label="Contraseña" name="adminPassword" onChange={handleChange} type="password" />
            <div>
              <button type="button" disabled={loading} onClick={handleSubmit}>
                {loading ? "Registrando..." : "Crear mi cuenta"}
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default Registro;
```

```css
/* Frontend/src/presentation/views/Registro/registro.module.css */
.registro__wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  padding: 24px;
}
.registro__form {
  max-width: 420px;
  width: 100%;
}
.header__registroInfo {
  margin-bottom: 24px;
}
.form__wrapper {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
```

Check `Frontend/src/components/Input/index.tsx`'s props first (read it — `Login/index.tsx` uses `isLabel`, `label`, `name`, `onChange`, `type`, and optionally `onKeyUp`) to confirm this usage matches its actual prop contract before assuming it's correct verbatim.

- [ ] **Step 6: Register the public route**

In `Frontend/src/infraestructure/Dashboard.tsx`, add the import and a new top-level route (same nesting level as `Login`, `Facturacion`, etc. — outside `LayoutView`):

```tsx
import Registro from "../presentation/views/Registro";
```

and inside `<Routes>`, alongside `<Route path="/" element={<Login />} />`:

```tsx
          <Route path="/registro" element={<Registro />} />
```

A separate confirmation-landing route is also needed for the email link (`/confirmar-registro?userId=...&token=...` from Task 3's email body) — add a small companion view or extend `Registro` to detect `useSearchParams()` and call `confirmarRegistro` when both query params are present, showing a simple "cuenta confirmada, ya puedes iniciar sesión" message with a link to `/`. Use `react-router-dom`'s `useSearchParams` (already a dependency, used implicitly by `react-router-dom` throughout this app) inside `Registro`'s component body to branch between the signup form and the confirmation view based on whether `token`/`userId` are present in the URL — this avoids adding a second route.

- [ ] **Step 7: Manual verification**

Run the frontend locally (`npm run dev` inside `Frontend/`, check `Frontend/package.json` for the exact script name first) alongside the backend (`dotnet run` in `WEB_API/`), and:
1. Navigate to `/registro`, fill the form with a real rubro from the dropdown, submit.
2. Confirm the "revisa tu correo" screen appears.
3. Check the backend logs / a real Resend dashboard (if `Resend:ApiKey`/`Resend:FromEmail` are configured in local `appsettings.Development.json` for this test) that an email was attempted.
4. Manually hit `GET /api/tenant/confirmar-registro?userId=...&token=...` (copy the real values from Task 3/4's own test output or a debugger breakpoint, since no real email will arrive without real Resend credentials in a local dev run) and confirm it returns success.
5. Log in with the new admin's email/password at `/` and confirm it succeeds and lands on `/dashboard/productos` (per `Login/index.tsx`'s existing fallback redirect for "any authenticated user that doesn't match a legacy rule").

- [ ] **Step 8: Commit**

```bash
git add Frontend/src/presentation/views/Registro Frontend/src/redux/reducers/registro Frontend/src/infraestructure/Dashboard.tsx Frontend/src/redux/store.ts Frontend/src/redux/rootState.ts
git commit -m "feat: public self-service signup page

Calls the new /api/tenant/registro-publico and /api/tenant/confirmar-registro
endpoints. Same redux pattern (createReducer + thunks) already used by
auth.reducer.ts.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

(Adjust the exact file list above to whatever Step 1 found — `store.ts`/`rootState.ts` names may differ slightly.)

---

## Self-review notes

- **Spec coverage:** all 3 confirmed decisions from the spec are covered — live tenant registry (Task 1), self-service signup reusing the existing admin-user creation (Task 3), email verification via Identity + Resend (Tasks 2 & 4), public frontend page (Task 5). The spec's own correction (login already gates on `EmailConfirmed`/`Tenant.Activo`, no changes needed there) is respected — no task touches `AuthenticationRepository.cs`.
- **Placeholder scan:** no TBD/TODO left. The two spots requiring the implementer to verify something at run time (Task 3's exact `TenantKey` casing, Task 5's exact `Input`/store file names) are concrete "read this file, adjust to what you find" instructions with real fallback guidance, not vague deferrals.
- **Type consistency:** `RegistroPublicoPayload` (Task 3, Step 1) is the same shape the frontend's `registrarNegocio` thunk (Task 5) builds and posts. `IEmailService.EnviarAsync` (Task 2) matches exactly how `RegistroPublico` (Task 3) calls it. `ConfirmarRegistro(userId, token)` (Task 4) matches the query string the confirmation email (Task 3) links to and what the frontend's `confirmarRegistro` thunk (Task 5) sends.
