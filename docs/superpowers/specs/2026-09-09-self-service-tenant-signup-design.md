# Self-service tenant signup — design

Fecha: 2026-09-09
Estado: aprobado por el usuario en chat, pendiente de plan de implementación.

## Objetivo

Que cualquier negocio (barbería, carnicería, tienda de juguetes, etc. — cualquier
`Rubro` ya soportado) pueda registrarse solo en PuntoVenta, sin que un
SuperAdmin intervenga, y quedar operativo de inmediato: con su panel
utilizable y con la misma credencial (usuario/contraseña) lista para que su
tienda online (siguiendo el patrón que ya construimos para Tendy) se
autentique contra la API de PuntoVenta.

## Motivación / hallazgo clave

`POST /api/tenant/crear-tenant` y `crear-empresa` **ya existen** y escriben a
la base de datos (`WEB_API/Controllers/TenantController.cs`), pero:

1. Ambos requieren rol `SuperAdmin` — no son self-service.
2. **La resolución de tenant en cada request no lee de la base de datos.**
   `Infrastructure/TenantRegistry.cs` lee `TenantOptions:Tenants` desde
   `appsettings.json`, registrado como `AddSingleton` en `Program.cs:136` —
   se carga **una sola vez al iniciar la aplicación**. Un tenant creado por
   API no queda resoluble hasta editar el config y reiniciar el servidor.

El punto 2 es el bloqueo real: sin arreglarlo, ninguna forma de registro
(self-service o no) sirve de nada operativamente el mismo día.

## Decisiones confirmadas

| Pregunta | Decisión |
|---|---|
| ¿Nivel de self-service? | Total: formulario público, sin intervención de un SuperAdmin. |
| ¿Qué se crea automáticamente? | Un usuario admin (vía ASP.NET Identity) — **no** una API-key separada. |
| ¿Cómo se conecta el storefront de esa empresa a PuntoVenta? | Reutiliza el login de ese mismo usuario admin (mismo patrón que Tendy: `POST /api/autenticacion/token`, JWT cacheado). No se construye un sistema de API-keys aparte. |
| ¿Control anti-abuso? | Verificación de email antes de activar (no aprobación manual, no activación sin fricción). |
| ¿Dónde vive el formulario? | Dentro del Frontend actual de PuntoVenta (`Frontend/src`, React + Vite), como una ruta pública nueva. |
| ¿Proveedor de email? | Resend, vía llamada HTTP directa (`POST https://api.resend.com/emails`) — sin SDK, reutilizando el mismo proveedor que ya usa Tendy pero con su propia `RESEND_API_KEY` y dominio verificado. |

## Diseño

### 1. Arreglo de fondo: `TenantRegistry` en vivo

- Cambiar `ITenantRegistry`/`TenantRegistry` de leer `appsettings.json` a
  consultar las tablas `Tenant`/`Empresa` (`SpaContext`) directamente.
- Cambiar su ciclo de vida en DI de `AddSingleton` a `AddScoped` — una
  consulta por request. El volumen esperado de tenants no justifica una capa
  de caché (YAGNI); si en el futuro el volumen lo exige, se agrega caché con
  invalidación explícita al crear/activar un tenant, no antes.
- La consulta filtra `WHERE Activo = true` (columna ya existente, migración
  `AddActivoToTenant`) — un tenant no verificado no es resoluble.
- El resto de `TenantResolver.cs` no cambia: sigue resolviendo por claim de
  JWT, subdominio o header `Tenant` exactamente igual que hoy: solo cambia
  de dónde `GetTenants()` saca la lista.

### 2. Registro público

Nuevo endpoint `POST /api/tenant/registro-publico` (`[AllowAnonymous]`, en
`TenantController` o un controller nuevo `RegistroPublicoController` si se
prefiere separar lo público de lo administrativo — decisión de la fase de
plan, no bloquea el diseño).

Body: nombre del negocio, `RubroId`, datos de empresa (los ya opcionales de
`CreateEmpresaPayload`: teléfono, email, RUC, dirección, etc.), y
`AdminEmail`/`AdminPassword` para el usuario inicial.

Flujo interno (reutiliza servicios existentes, no se reescriben):
1. `TenantRepository.CreateTenant(nombre, rubroId, configuracion)` → crea el
   `Tenant` con `Activo = false`.
2. `CreateEmpresa` con el `TenantId` recién creado.
3. Crear el usuario admin vía `UserManager<User>.CreateAsync(...)` con
   `EmailConfirmed = false`, asociado al tenant/empresa.
4. `UserManager.GenerateEmailConfirmationTokenAsync(user)` → token.
5. Enviar correo (ver sección 4) con link a
   `GET /api/tenant/confirmar-registro?userId=...&token=...`.

### 3. Confirmación y activación

`GET /api/tenant/confirmar-registro` (`[AllowAnonymous]`):
1. `UserManager.ConfirmEmailAsync(user, token)` — validación de token nativa
   de Identity, no se reinventa.

**Ya no hace falta tocar el login.** Se verificó `AuthenticationRepository.cs`
directamente: `Token()` ya rechaza el login si `user.EmailConfirmed ==
false` (líneas 82-87, mensaje "Por favor, verifique su cuenta...") y ya
rechaza tenants con `Activo == false` (líneas 69-75). Ambos gates ya
existen — el diseño original de esta sección asumía que había que
construirlos; no hace falta.

**Corrección al punto 2 (ya no aplica tal cual):** dado que el login ya
bloquea por `EmailConfirmed`, **no hace falta que `Tenant.Activo` empiece en
`false`** — puede quedar en su valor por defecto (`true`, ya es el default
de la entidad `Tenant`). El gate real de "no operativo hasta confirmar" lo
da `EmailConfirmed` en el usuario, no `Tenant.Activo`. Esto simplifica el
paso 1 del flujo de registro: no hay que tocar `TenantRepository.CreateTenant`
en absoluto.

### 3.1 El usuario admin ya se crea automáticamente — solo hay que ajustar sus valores para el caso self-service

`TenantRepository.CreateEmpresa` (`Infrastructure/Repositories/TenantRepository.cs:30-79`)
ya llama a un método privado `CreateUserAdmin(username, identificador)`
(línea 128-148) que crea el usuario vía `UserManager.CreateAsync`. Hoy lo
hace con valores fijos pensados para cuando un SuperAdmin de confianza crea
la empresa manualmente: `Email = ""`, `Password = "123456"`,
`EmailConfirmed = true`.

Para el flujo público hace falta un email y contraseña reales, y
`EmailConfirmed = false` hasta confirmar. La forma más chica de lograrlo sin
romper el flujo administrativo existente (`crear-empresa`,
`add-empresa`, que deben seguir comportándose exactamente igual): agregar
parámetros opcionales a `CreateUserAdmin` (`email`, `password`,
`emailConfirmed`) con los valores actuales como default, y que el nuevo
flujo público de registro sea el único que pasa valores explícitos.

### 4. `IEmailService` (nuevo, no existe ningún servicio de correo hoy)

Interfaz mínima:

```csharp
public interface IEmailService
{
    Task EnviarAsync(string destinatario, string asunto, string htmlBody);
}
```

Implementación: `HttpClient` haciendo `POST` a
`https://api.resend.com/emails` con `Authorization: Bearer
{RESEND_API_KEY}`, body `{ from, to, subject, html }`. Configuración nueva
en `appsettings.json` / variables de entorno: `RESEND_API_KEY` (propia de
PuntoVenta, no la de Tendy) y el remitente verificado (ej.
`notificaciones@<dominio-de-puntoventa>`).

### 5. Pantalla pública (Frontend)

Nueva ruta pública (ej. `/registro`) en `Frontend/src/presentation/views`,
fuera del layout autenticado (`LayoutView`) — mismo nivel que `Login` en
`Dashboard.tsx`. Formulario: nombre del negocio, rubro (selector, ya hay
catálogo de rubros vía `GET /api/extensiones/rubros`), datos de empresa,
email/contraseña del admin. Tras enviar, pantalla de "revisa tu correo para
activar tu cuenta".

## Fuera de alcance (explícitamente, por decisión del usuario)

- Sistema de API-keys por empresa para integraciones tipo storefront — se
  usa login normal de usuario en su lugar.
- Aprobación manual o rate-limiting anti-abuso más allá de la verificación
  de email — se puede agregar después si hay abuso real (YAGNI).
- Migrar tenants existentes (los que hoy viven solo en `appsettings.json`,
  si los hay) a la tabla `Tenant` — se asume que los tenants actuales ya
  están en la base de datos (dado que `TenantService.CreateTenant` ya
  escribe ahí); si algún tenant vive *solo* en config sin fila en `Tenant`,
  eso se descubre y resuelve en la fase de plan/implementación, no aquí.

## Riesgos / decisiones pendientes para el plan de implementación

1. **Rendimiento de `TenantRegistry` en `AddScoped`:** confirmar que una
   consulta por request no introduce latencia perceptible — el volumen de
   tenants es pequeño hoy, pero vale medir antes de dar por cerrado.
2. **Formato exacto del email de confirmación** (HTML, copy) — pendiente de
   redactar en el plan.
3. **Verificar si existen tenants "huérfanos"** que solo viven en
   `appsettings.json` sin fila correspondiente en la tabla `Tenant`, antes
   de cambiar `TenantRegistry` a leer de la base de datos — si existen,
   hay que migrarlos primero o el cambio los dejaría inoperativos.

## Siguiente paso

Pasar a `superpowers:writing-plans` para el plan de implementación fase por
fase: 1) `TenantRegistry` en vivo, 2) `IEmailService` + confirmación de
Identity, 3) endpoint de registro público, 4) bloqueo de login sin
confirmar, 5) pantalla pública en el Frontend.
