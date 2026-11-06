# Identidad de la suite (paso 1 estilo Zoho) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el login de PuntoVenta (ARP) sirva como login único de la suite: el token dice qué apps tiene contratadas la empresa (`apps`) y se firma con RS256 para que otras apps (ARM) lo validen con la clave pública, sin romper los tokens ni el login actuales.

**Architecture:** Todo dentro del backend actual (no se mueve el código de login existente). Se agrega: (1) tabla `TenantApp` con las apps contratadas por empresa, leída al emitir el token; (2) un `FirmadorToken` que firma con RSA si hay clave configurada y si no sigue con HMAC, y valida ambos durante la transición; (3) endpoint público JWKS con solo la clave pública. Nombre del módulo en código: **Identidad** (no "Cuentas": en este repo "Cuentas" ya es el plan de cuentas contable).

**Tech Stack:** .NET 6, EF Core 6 + Npgsql (tests con SQLite en memoria), ASP.NET Identity, `System.IdentityModel.Tokens.Jwt`, xUnit.

**Spec:** decisión registrada en memoria del proyecto "Suite estilo Zoho" (Cuentas/login central + ARP + ARM) y conversación del 2026-11-04. Este plan cubre solo el paso 1 (identidad dentro del backend actual). Fuera de alcance: mover el login a un servicio aparte, selector de apps en el frontend, que PuntoVenta rechace tokens sin `arp`, ARM.

## Global Constraints

- El login actual (`POST /api/autenticacion/token`, `cambiar-empresa`) debe seguir respondiendo igual; solo se agrega el claim `apps`.
- Tokens HMAC emitidos antes del despliegue deben seguir siendo válidos hasta que expiren.
- Si la variable `TokenManagement:RsaPrivateKeyPem` no está configurada, el comportamiento es exactamente el de hoy (HMAC).
- Los tokens de clientes de la tienda (esquema `Cliente`, `ClienteAuthService`) no cambian.
- Códigos de app: `"arp"` (PuntoVenta) y `"arm"` (CRM). Claim: `"apps"`, valor JSON array de strings, ej. `["arp"]`.
- Empresa sin filas en `TenantApp` ⇒ `["arp"]` (compatibilidad: todas las empresas actuales y las que se registren).
- El JWKS nunca expone parámetros privados de la clave.

## Review Focus

- Empresa existente sin filas en `TenantApp` → el token trae `apps=["arp"]` (test en Task 1).
- Empresa con `arm` activo y `arp` desactivado → `["arm"]`; filas inactivas no cuentan (test en Task 1).
- Token HMAC emitido antes del despliegue, presentado después de configurar RSA → sigue válido (test en Task 2).
- Despliegue sin la variable PEM → firma HMAC y JWKS devuelve `keys: []` sin error (test en Task 2).
- JWKS con RSA configurado → no contiene `d`, `p`, `q`, `dp`, `dq`, `qi` (test en Task 2).

---

### Task 1: Apps contratadas por empresa y claim `apps`

**Files:**
- Create: `Backend/Domain/Entities/TenantApp.cs`
- Create: `Backend/Infrastructure/Configuration/TenantAppConfiguration.cs`
- Create: `Backend/Infrastructure/Repositories/TenantAppRepository.cs`
- Modify: `Backend/Infrastructure/Data/SpaContext.cs` (DbSet + `ApplyConfiguration`, junto a los demás)
- Modify: `Backend/Domain/Tenant/ClaimConstants.cs` (agregar `Apps`)
- Modify: `Backend/Infrastructure/Repositories/AuthenticationRepository.cs:138-193` (`EmitirToken` agrega el claim)
- Modify: `Backend/Infrastructure/DependencyInjection.cs` (registrar repo, mismo estilo que `.AddScoped<TiendaRepository>()`)
- Modify: `Backend/WEB_API/Controllers/TenantController.cs` (endpoint superadmin)
- Create: migración `Backend/Infrastructure/Migrations/*_IdentidadTenantApp.cs`
- Test: `Backend/Tests/Infrastructure.Tests/TenantAppRepositoryTests.cs`

**Interfaces:**
- Produces:
  - `class TenantApp { int Id; int TenantIdentificador; string App; bool Activo; DateTime Desde; }` — índice único `(TenantIdentificador, App)`. Sin query filter (se lee en el login, antes de tener tenant en contexto).
  - `const string ClaimConstants.Apps = "apps"`
  - `class TenantAppRepository(SpaContext)`:
    - `Task<List<string>> AppsContratadas(int tenantIdentificador)` — filas activas ordenadas por `App`; sin ninguna fila ⇒ `["arp"]`.
    - `Task<(ServiceStatus, string)> SetApp(int tenantIdentificador, string app, bool activo)` — upsert; `app` fuera de `{"arp","arm"}` ⇒ `FailedValidation`; tenant inexistente ⇒ `NotFound`. La primera vez que se toca una empresa sin filas, crear antes la fila `arp` activa (para que activar `arm` no le quite `arp`).
  - `PUT /api/tenant/superadmin/{identificador}/apps` body `{ "app": "arm", "activo": true }`, `[Authorize(Roles = "SuperAdmin")]`.

- [ ] **Step 1: Write the failing tests** en `TenantAppRepositoryTests.cs` usando `TestDbContextFactory.CreateContext()` y sembrando un `Tenant` (Identificador 1, Name "TEST", RubroId del seed existente):
  - `SinFilas_DevuelveSoloArp` → `AppsContratadas(1)` == `["arp"]`
  - `ActivarArm_EnEmpresaSinFilas_ConservaArp` → `SetApp(1,"arm",true)` luego `AppsContratadas(1)` == `["arm","arp"]`
  - `DesactivarArp_DejaSoloArm` → tras lo anterior `SetApp(1,"arp",false)` ⇒ `["arm"]`
  - `AppDesconocida_EsFailedValidation` → `SetApp(1,"xyz",true).Item1 == ServiceStatus.FailedValidation`
  - `TenantInexistente_EsNotFound` → `SetApp(99,"arm",true).Item1 == ServiceStatus.NotFound`

- [ ] **Step 2: Run tests to verify they fail**
  Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter TenantAppRepositoryTests`
  Expected: FAIL de compilación (`TenantApp` / `TenantAppRepository` no existen).

- [ ] **Step 3: Implement** entidad, configuración (`ToTable("TenantApp")`, `App` max 20, índice único), DbSet, repositorio y registro en DI.

- [ ] **Step 4: Run tests to verify they pass** (mismo comando). Expected: 5 PASS.

- [ ] **Step 5: Claim en el token.** En `EmitirToken`, después del claim `rutas`, agregar `new Claim(ClaimConstants.Apps, JsonConvert.SerializeObject(await _tenantApps.AppsContratadas(tenant.Identificador)))`. Inyectar `TenantAppRepository` en el constructor de `AuthenticationRepository`. Como `cambiar-empresa` usa el mismo `EmitirToken`, recalcula las apps de la empresa nueva sin cambios extra.

- [ ] **Step 6: Endpoint superadmin** en `TenantController` (inyectar `TenantAppRepository`; payload `SetTenantAppPayload { string App; bool Activo; }` en `Domain/Payloads`), devolviendo `BadRequest`/`NotFound`/`Ok` según `ServiceStatus`.

- [ ] **Step 7: Migración**
  Run: `dotnet ef migrations add IdentidadTenantApp --project Backend/Infrastructure --startup-project Backend/WEB_API`
  Expected: migración que solo crea la tabla `TenantApp` y su índice (revisar que no traiga cambios ajenos).

- [ ] **Step 8: Suite completa**
  Run: `dotnet test Backend/Tests/Infrastructure.Tests`
  Expected: mismos resultados que antes + 5 nuevos PASS.

- [ ] **Step 9: Commit**
  ```bash
  git add Backend/Domain Backend/Infrastructure Backend/WEB_API Backend/Tests
  git commit -m "feat: apps contratadas por empresa (TenantApp) y claim apps en el token"
  ```

### Task 2: Firma RS256 con transición desde HMAC y JWKS público

**Files:**
- Create: `Backend/Infrastructure/Identidad/FirmadorToken.cs`
- Modify: `Backend/Domain/Models/TokenManagement.cs` (agregar `string? RsaPrivateKeyPem`)
- Modify: `Backend/WEB_API/Program.cs:108-130` (validación del esquema `Staff`)
- Modify: `Backend/Infrastructure/Repositories/AuthenticationRepository.cs:197-222` (usar el firmador)
- Create: `Backend/WEB_API/Controllers/JwksController.cs`
- Test: `Backend/Tests/Infrastructure.Tests/FirmadorTokenTests.cs`

**Interfaces:**
- Consumes: nada de Task 1.
- Produces:
  - `class FirmadorToken(string secretKeyHmac, string? rsaPrivateKeyPem)` (singleton):
    - `SigningCredentials Credenciales()` — RS256 (`SecurityAlgorithms.RsaSha256`) si hay PEM; si no, HMAC como hoy (`Encoding.ASCII.GetBytes(secretKey)`, `HmacSha256Signature`).
    - `IEnumerable<SecurityKey> ClavesValidacion()` — siempre la HMAC + la RSA pública si hay PEM.
    - `JsonWebKeySet Jwks()` — solo la clave pública RSA (`JsonWebKeyConverter.ConvertFromRSASecurityKey` sobre una `RsaSecurityKey` creada con `ExportParameters(false)`), `use="sig"`, `alg="RS256"`; sin PEM ⇒ conjunto vacío.
    - `KeyId` de la RSA = primeros 16 hex del SHA-256 del módulo (estable entre reinicios, así el `kid` del token coincide con el JWKS).
  - `GET /.well-known/jwks.json` `[AllowAnonymous]` ⇒ `firmador.Jwks()`.

- [ ] **Step 1: Write the failing tests** en `FirmadorTokenTests.cs` (generar PEM en el test con `new string(PemEncoding.Write("RSA PRIVATE KEY", RSA.Create(2048).ExportRSAPrivateKey()))` — `ExportRSAPrivateKeyPem()` no existe en .NET 6; validar con `JwtSecurityTokenHandler.ValidateToken` y `TokenValidationParameters { IssuerSigningKeys = ..., ValidateIssuer=false, ValidateAudience=false }`):
  - `ConRsa_TokenFirmadoValida_SoloConClavePublicaDelJwks` → firmar con `Credenciales()`, validar usando únicamente las claves de `Jwks().GetSigningKeys()` ⇒ OK y header `alg == "RS256"`.
  - `ConRsa_TokenHmacAnterior_SigueValido` → token firmado con un `FirmadorToken(secret, null)`, validado con `ClavesValidacion()` de `FirmadorToken(secret, pem)` ⇒ OK.
  - `SinRsa_FirmaHmac_YJwksVacio` → `Credenciales().Algorithm == SecurityAlgorithms.HmacSha256Signature` y `Jwks().Keys.Count == 0`.
  - `Jwks_NoExponeParametrosPrivados` → la única clave tiene `D`, `P`, `Q`, `DP`, `DQ`, `QI` nulos y `Kid` no vacío.
  - `ConRsa_TokenFirmadoConOtraClaveRsa_NoValida` → firmado con otro PEM ⇒ lanza `SecurityTokenSignatureKeyNotFoundException` o `SecurityTokenInvalidSignatureException`.

- [ ] **Step 2: Run tests to verify they fail**
  Run: `dotnet test Backend/Tests/Infrastructure.Tests --filter FirmadorTokenTests`
  Expected: FAIL de compilación (`FirmadorToken` no existe).

- [ ] **Step 3: Implement `FirmadorToken`** (`RSA.ImportFromPem` para cargar la clave).

- [ ] **Step 4: Run tests to verify they pass.** Expected: 5 PASS.

- [ ] **Step 5: Conectar.** En `Program.cs` crear el `FirmadorToken` con `TokenManagement:SecretKey` y `TokenManagement:RsaPrivateKeyPem`, registrarlo como singleton y en `tokenValidationParameters` reemplazar `IssuerSigningKey` por `IssuerSigningKeys = firmador.ClavesValidacion()`. `clienteTokenValidationParameters` se clona después igual que hoy (acepta ambas claves; la audiencia sigue separando cliente de staff). En `EmitirToken` usar `_firmador.Credenciales()` en lugar del `SigningCredentials` HMAC. No tocar `ClienteAuthService`.

- [ ] **Step 6: Endpoint JWKS** `JwksController` con ruta absoluta `[Route("/.well-known/jwks.json")]`.

- [ ] **Step 7: Verificación manual local**
  1. Sin variable PEM: levantar API, hacer login, decodificar el token en jwt.io ⇒ `alg: HS256`, trae `apps: ["arp"]`; `GET /.well-known/jwks.json` ⇒ `{"keys":[]}`.
  2. Generar clave: `openssl genrsa 2048` y ponerla en `TokenManagement__RsaPrivateKeyPem` (Railway: variable con saltos de línea). Reiniciar: un token emitido en el paso 1 sigue funcionando en `GET /api/autenticacion/@me`; un login nuevo da `alg: RS256` con `kid` igual al del JWKS.

- [ ] **Step 8: Suite completa**
  Run: `dotnet test Backend/Tests/Infrastructure.Tests` — Expected: sin regresiones.

- [ ] **Step 9: Commit**
  ```bash
  git add Backend/Domain Backend/Infrastructure Backend/WEB_API Backend/Tests
  git commit -m "feat: firma RS256 del token con transicion desde HMAC y JWKS publico"
  ```

## Después de este plan (no incluido)

- Configurar la variable PEM en producción (Railway) y, pasado el tiempo de expiración del refresh token, dejar de aceptar HMAC para staff.
- ARM valida tokens contra `/.well-known/jwks.json` y exige `"arm"` en `apps`.
- Selector de apps en el frontend leyendo el claim `apps`.
