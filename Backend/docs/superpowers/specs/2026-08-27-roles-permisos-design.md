# Roles y permisos por tenant — diseño

## Contexto

PuntoVenta es un POS multi-tenant (Backend .NET 6 / EF Core / Postgres, Frontend React + Redux). Hoy, varias decisiones de acceso (a qué pantalla aterriza un usuario tras el login, qué botones ve, qué categorías de productos puede filtrar) están resueltas con cadenas de `if` sobre el username literal — algunas comparando contra usernames de clientes específicos (`JRAMIREZ`, `DISCOBAR`, `SPASOLIS`), repartidas entre `Login/index.tsx`, `Sidebar/Navbar/index.tsx` y `Facturacion/ProductosFiltradosByCard/index.tsx`.

Ya existe un sistema de permisos por módulo/submódulo (`AspNetModule` → `AspNetSubModule` → `AspNetUserSubModule`) cuyo resultado viaja en el JWT como claim `"rutas"` y que el `Sidebar` usa para filtrar el menú. Pero en la práctica **solo el usuario admin de cada tenant recibe submódulos** (`TenantRepository.AsociarModuleUser` le otorga automáticamente el catálogo completo al crear la empresa); ningún flujo asigna submódulos a un empleado. Los empleados restringidos (VENTAS1, RECEPCION, CONTADORA, etc.) no tienen ningún `AspNetUserSubModule`, así que su único control de acceso real hoy son los checks de username hardcodeados.

El rol de ASP.NET Identity (`Role`/`UserRol`) existe en el esquema y el código de emisión del JWT ya sabe leerlo (`_userManager.SupportsUserRole` → agrega `ClaimTypes.Role`), pero nunca se crea ni se asigna ningún rol en ningún lado — está muerto.

Además, hay un segundo set de entidades `Module`/`Submodule`/`UserSubmodule` (sin prefijo `AspNet`) que no tiene `DbSet` registrado en `SpaContext`: código muerto, nunca llegó a usarse.

## Objetivo

Que el administrador de cada tenant pueda crear sus propios roles, decidir qué pantallas ve cada rol, y asignar roles a sus usuarios — sin que ningún comportamiento dependa de comparar el username contra una lista fija en el código.

## Alcance

Dentro:
- Modelo de datos: `Role` gana `TenantId`, `RutaPorDefecto`, `Prioridad`; tabla nueva `RoleSubmodule`.
- Reemplazo de la asignación directa Usuario↔Submódulo por Usuario↔Rol↔Submódulo.
- Resolución de acceso (menú visible + ruta de aterrizaje tras login) basada en los roles del usuario.
- Pantalla nueva de administración de roles (CRUD + asignación de submódulos).
- Selector de roles en la pantalla de creación/edición de usuarios.
- Pantalla "sin permisos" para usuarios sin ningún rol asignado.
- Migración de los tenants existentes: rol "Admin" auto-creado con el catálogo completo, asignado a los usuarios que hoy tienen todos los submódulos. Los empleados sin submódulos hoy quedan sin rol (el admin de cada tenant los asigna manualmente, es el flujo que se está construyendo).
- Reemplazo de los tres puntos de código que hoy comparan username: `Login/index.tsx`, `Sidebar/Navbar/index.tsx`, `Facturacion/ProductosFiltradosByCard/index.tsx`.
- Borrado de las entidades muertas `Module`/`Submodule`/`UserSubmodule` (sin prefijo `AspNet`).

Fuera de alcance (decisión explícita, YAGNI):
- Generalizar la restricción de categoría/grupo de VENTAS1/VENTAS2 a un mecanismo de datos por rol. Se migra el check de `username === 'VENTAS1'` a `roles.includes('Ventas')` únicamente; si aparece un segundo caso de "qué datos ve" (no "qué pantalla ve"), se diseña aparte.
- Roles compartidos entre tenants o roles "de plataforma". Cada rol pertenece a un único tenant.
- Jerarquía de roles (un rol que herede permisos de otro).

## Modelo de datos

**`Role`** (hoy `IdentityRole<string>` sin más campos; se agregan):

| Campo | Tipo | Notas |
|---|---|---|
| `TenantId` | `string` (FK a `Tenant.Name`, mismo patrón que el resto de `EntityBase`) | Un rol pertenece a un solo tenant. Query filter igual al resto de entidades scoped por tenant. |
| `RutaPorDefecto` | `string` | Ruta del frontend a la que aterriza un usuario con este rol tras el login (ej. `/facturacion`). |
| `Prioridad` | `int` | Desempate de `RutaPorDefecto` cuando un usuario tiene varios roles. Gana el número más bajo. |

`Role` no hereda de `EntityBase` (es `IdentityRole<string>`), así que `TenantId` se agrega como columna propia, no vía la convención de `EntityBase`; el query filter se define igual que los demás (`TenantId == _tenant.Name`).

**`RoleSubmodule`** (tabla nueva, análoga a `AspNetUserSubModule` pero Rol↔Submódulo):

```
RoleId       string  FK -> Role.Id
SubmoduleId  string  FK -> AspNetSubModule.Identificador
```

Con `EntityBase` (TenantId, auditoría) igual que las demás tablas de asignación.

**`UserRol`** (ya existe, `IdentityUserRole<string>`, hoy sin filas): pasa a ser el único lugar donde se define qué puede ver un usuario. Relación muchos-a-muchos ya soportada nativamente por ASP.NET Identity (`_userManager.AddToRoleAsync` / `RemoveFromRoleAsync`).

**`AspNetUserSubModule`** dejará de recibir escrituras nuevas (ya no se asigna directo a usuario). La tabla no se borra en esta fase — se deja de usar y, si en el futuro se confirma que no hace falta, se elimina en una migración aparte. Motivo: menor riesgo, no bloquea el resto del trabajo.

## Resolución de acceso

Al hacer login (`AuthenticationRepository.Token`), en vez de calcular `rutas` desde `ApplicationUserDto.Resumen` (que lee `UserSubmodules` directo), se calcula:

1. Cargar los roles del usuario (`_userManager.GetRolesAsync` ya se usa para los claims `ClaimTypes.Role`; se reutiliza para resolver los objetos `Role` completos).
2. `rutas` = unión (sin duplicados) de los submódulos de `RoleSubmodule` de todos los roles del usuario, agrupados por módulo — mismo shape que hoy (`AccesosDetalle` con `Modulo`/`ModuloNombre`/`SubModulos`), para no romper el `Sidebar`.
3. `rutaPorDefecto` = `RutaPorDefecto` del rol con menor `Prioridad` entre los roles del usuario. Se agrega como claim nuevo en el JWT (`"rutaPorDefecto"`).
4. Si el usuario no tiene ningún rol: `rutas` queda vacío y `rutaPorDefecto` no se emite.

En el frontend, `Login/index.tsx` reemplaza toda la cadena de `if (username === ...)` por:

```
if (me?.rutaPorDefecto) {
  window.location.href = me.rutaPorDefecto;
} else {
  window.location.href = "/sin-permisos";
}
```

`/sin-permisos` es una pantalla nueva y simple: "Bienvenido a `{empresa}`" (el nombre ya viaja en el claim `empresa`) + un mensaje de que no tiene permisos asignados y que contacte al administrador del sistema.

## API nueva (backend)

Patrón idéntico al resto del código (`I<X>Repository` / `I<X>Service` / `<X>Controller`, tuplas `(ServiceStatus, T, string)`):

- `GET /api/roles` — lista los roles del tenant actual (nombre, ruta, prioridad, cantidad de usuarios asignados).
- `GET /api/roles/{id}` — detalle de un rol con sus submódulos marcados.
- `POST /api/roles` — crea un rol: `{ nombre, rutaPorDefecto, prioridad, submoduleIds[] }`.
- `PUT /api/roles/{id}` — edita cualquiera de esos campos.
- `DELETE /api/roles/{id}` — falla con `FailedValidation` si el rol tiene usuarios asignados (mensaje claro, no genérico).
- `GET /api/roles/catalogo-submodulos` — reexpone `UsersRepository.GetModules()` (ya existe, sin cambios) para pintar el árbol de checkboxes.
- `PUT /api/usuarios/{id}/roles` — reemplaza el set completo de roles de un usuario: `{ roleIds[] }`. Internamente usa `RemoveFromRolesAsync` + `AddToRolesAsync` de `UserManager`.

## Frontend

**Pantalla nueva "Roles"** (Admin → mismo patrón de layout que Productos/Usuarios: tabla + modal crear/editar):
- Tabla: nombre, ruta de aterrizaje, prioridad, cantidad de usuarios.
- Modal: nombre, select de ruta de aterrizaje (no texto libre, para evitar rutas rotas — las opciones salen de la configuración de rutas del router del frontend, no de una lista hardcodeada en este spec), input de prioridad, árbol de checkboxes de módulos/submódulos (mismo catálogo que ya se usa en la asignación actual de submódulos por usuario, si existe una UI parecida se reutiliza su estructura de árbol).

**Pantalla de usuarios existente:** se agrega un multi-select de roles del tenant al crear/editar un usuario.

**Los tres puntos hardcodeados:**

1. `Login/index.tsx`: se borra toda la cadena de ifs (líneas ~91-148), reemplazada por la lógica de dos ramas descrita arriba.
2. `Sidebar/Navbar/index.tsx:95`: `!me?.userName?.startsWith('RECEPCION') && !me?.userName?.startsWith('CONTADORA')` → `!me?.roles?.includes('Recepcion') && !me?.roles?.includes('Contadora')`.
3. `Facturacion/index.tsx:106`: `me?.userName.startsWith('RECEPCION') || me?.userName.startsWith('CONTADORA')` → `me?.roles?.includes('Recepcion') || me?.roles?.includes('Contadora')`.
4. `Facturacion/ProductosFiltradosByCard/index.tsx:57,64`: `me?.userName === 'VENTAS1' || me?.userName === 'VENTAS2'` → `me?.roles?.includes('Ventas')`.

Esto requiere que el JWT/estado de auth exponga `me.roles` (lista de nombres de rol) además de `me.rutas` — ya se agregan como `ClaimTypes.Role` en el backend, solo falta que el frontend los lea del token igual que lee los demás claims.

## Migración de datos existentes

Migración de EF Core + un método de datos (mismo estilo que `PuntoVentaDbContextData.SeedCatalogoTenant`, corrido una vez al arrancar o vía un endpoint de mantenimiento, a decidir en el plan de implementación):

Por cada tenant:
1. Si no existe, crear un rol `"Admin"` con `Prioridad = 0`, `RutaPorDefecto = "/dashboard/productos"`, y todos los submódulos del catálogo en `RoleSubmodule`.
2. Para cada usuario del tenant que hoy tenga el catálogo completo de submódulos en `AspNetUserSubModule` (o cualquier registro, dado que hoy solo el admin recibe alguno), asignarle el rol `"Admin"` vía `UserRol`.
3. Los usuarios sin ningún `AspNetUserSubModule` (los empleados restringidos hoy manejados por username) quedan sin rol tras la migración. No se intenta adivinar a qué rol corresponden — el admin de cada tenant los asigna manualmente desde la pantalla nueva.

`TenantRepository.AsociarModuleUser` y `TenantRepository.ReasignarModulos` se reemplazan: en vez de otorgar submódulos sueltos al usuario nuevo, crean (si no existe) el rol `"Admin"` del tenant y se lo asignan al usuario.

## Limpieza

Se eliminan las entidades muertas `Domain/Entities/Identity/Module.cs`, `Submodule.cs` y `UserSubmodule.cs` (sin prefijo `AspNet`, sin `DbSet`, sin ninguna referencia funcional) y su configuración si la tuvieran. Confirmar con un grep final antes de borrar que efectivamente no quedó ninguna referencia.

## Testing

Mismo patrón que el resto de la suite (`Backend/Tests/Infrastructure.Tests`, SQLite en memoria):

- Resolución de `rutas`: un usuario con dos roles ve la unión de los submódulos de ambos, sin duplicados.
- Resolución de `rutaPorDefecto`: con dos roles de distinta prioridad, gana el de menor número.
- Usuario sin roles: `rutas` vacío, sin `rutaPorDefecto`.
- `DELETE /api/roles/{id}` con usuarios asignados: falla con `FailedValidation`, no borra nada.
- Migración: un tenant con un usuario "full-submódulos" y otro sin ninguno → el primero termina con el rol Admin asignado, el segundo sin roles.

## Riesgos / cosas a validar en el plan

- El claim `"rutas"` cambia de fuente (de `UserSubmodules` directo a la unión vía roles) pero mantiene el mismo shape — el `Sidebar` no debería necesitar cambios más allá de leer `me.roles` para los tres puntos hardcodeados.
- Migrar en producción implica que, hasta que cada tenant admin reasigne roles a sus empleados restringidos, esos empleados van a caer en `/sin-permisos` en vez de a su pantalla actual. Vale la pena avisar a los tenants antes de desplegar, o hacer la migración y el deploy del frontend en el mismo paso para no dejar una ventana con comportamiento a medias.
