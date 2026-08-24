# Arquitectura Escalable — Multi-Empresa · Multi-Sede · Multi-País · Multi-Rubro

> Documento técnico de diagnóstico y diseño para convertir el actual POS mono-negocio
> (**SOLIS SALON & SPA**, RUC 10430936315 — Perú) en una plataforma SaaS multi-tenant que
> sirva a droguerías, barberías, spas, consultorios (kinesiólogo/kinesióloga), ginecología,
> tiendas y cualquier rubro; con múltiples empresas, múltiples sedes, múltiples países y
> un solo código base.

---

## A. Diagnóstico técnico del estado actual

### A.1 Stack

| Capa | Tecnología | Evidencia |
|---|---|---|
| Backend API | ASP.NET Core Web API (.NET) + Kestrel `:5001` | `WEB_API/Program.cs` |
| Backend arquitectura | Clean-ish (Domain / Application / Infrastructure / WEB_API, solución `WEB_API_SPA.sln`, 4 csproj) | `Backend/WEB_API_SPA.sln` |
| ORM | EF Core + Npgsql (PostgreSQL), migraciones | `Infrastructure/Data/SpaContext.cs`, `Infrastructure/Migrations/` |
| Auth | ASP.NET Identity + JWT Bearer (HS256), opcional refresh token | `Program.cs`, `Infrastructure/Repositories/AuthenticationRepository.cs` |
| Programación de tareas | Coravel (job SUNAT) | `WEB_API/Jobs/InvoiceJob.cs` |
| Frontend | React 18 + Vite + TypeScript, Redux Toolkit + thunk, react-query, Tailwind, styled-components, framer-motion | `Frontend/package.json` |
| Facturación electrónica externa | `apisperu.com` (edición de comprobantes peruanos UBL 2.1) | `Application/Proxies/FacturacionProxy.cs` |

### A.2 Lo que YA existe y es reutilizable (activos)

1. **`EntityBase.TenantId`** (`Domain/Entities/EntityBase.cs:15`) — toda entidad de negocio hereda `TenantId`, `UsuarioCreacion`, `FechaCreacion`, `Estado`.
2. **Filtros de tenant en el contexto EF** — `SpaContext.OnModelCreating` aplica `HasQueryFilter(e => e.TenantId == _tenant.Name)` a ~17 entidades (`SpaContext.cs:55-75`).
3. **Auto-asignación de tenant al guardar** — `SpaContext.SaveChangesAsync` inyecta `TenantId = _tenant.Name` y `UsuarioCreacion` (`SpaContext.cs:108-125`).
4. **Resolver + registry esqueléticos** — `ITenantResolver/TenantRegistry` (`Infrastructure/TenantResolver.cs`, `TenantRegistry.cs`); detección de subdominio ya implementada pero **desactivada**.
5. **Modelo OEM multi-marca** — `Tenant`, `Empresa`, `EmpresaTenant`, `Rubro` + controlador `TenantController` (`api/tenant/*`) y `CargaInicialController` (siembra por `RubroId`).
6. **RBAC por módulos** — `AspNetModule`, `AspNetSubModule`, `AspNetUserSubModule`, claims `rutas` en el JWT; el menú Sidebar ya se filtra por `me.rutas`.
7. **Proxys HTTP reutilizables** — `BaseService.SendAsync<T>` genérico (`Application/Services/BaseService/BaseService.cs`).
8. **Serie/correlativo** — entidad `Seriecorrelativo` con filtro de tenant (base para multi-serie).

### A.3 Hallazgos críticos (limitantes para la escalabilidad)

#### H1. La empresa fiscal está hardcodeada (bloquea multi-empresa)
- `WEB_API/Jobs/InvoiceJob.cs:98-222` — `ArmarInvoice()` / `ArmarVoided()` incrustan RUC `10430936315`, "SOLIS EGUIZABAL VENTURA", dirección Los Olivos, y el tenant fijo **"SPASOLIS1"** (líneas 42, 64, 71).
- `Infrastructure/Repositories/ComprobanteRepository.cs:382-407, 468-541` — `GeneratePdfRequest()` filtra `TenantId == "SPASOLIS1"` y **duplica** la empresa en su `ArmarInvoice()`.
- `Application/Proxies/FacturacionProxy.cs:29,39,50` — token JWT de apisperu **incrustado en el código fuente**; no es configurable por tenant.

#### H2. El multi-tenant real no está activado (bloquea multi-empresa multi-país)
- `TenantResolver.GetCurrentTenant()` (`TenantResolver.cs:27-97`): el código de subdominio/registry está **comentado** y siempre retorna `DefaultConnection` (`appsettings.json`).
- `SpaContext` (ctor) solo usa la connection string si el resolver la entrega → hoy **1 sola BD para todos**; el camino "DB por tenant" es muerto (`SpaContext.cs:21-23`).
- `DatabaseHelper.cs` tiene 2 connection strings hardcodeadas distintas entre sí y a `appsettings` (ruido/riesgo).
- Efecto colateral: los correlativos de `Seriecorrelativo` no están aislados por sucursal, solo por tenant.

#### H3. No existe concepto de SEDE (bloquea multi-sede)
- No hay entidad `Sucursal` ni columna `SucursalId` en ninguna entidad. La caja (`Caja.cs`) no referencia sede; `CajaRepository` y reportes operan a nivel tenant/usuario.
- Del frontend se llama `/extensiones/sucursales` (reducer `my-business`) pero **ese endpoint no existe** en `ExtensionesController`.

#### H4. El país está fijo a Perú (bloquea multi-país)
- **Moneda**: `"PEN"` y símbolo `S/` hardcodeados en `InvoiceJob.cs:134` y en toda la UI.
- **Impuesto**: IGV 18% incrustado en `ComprobanteRepository.cs:73-75,115` y en `InvoiceJob.cs:150-157` (`porcentajeIgv = 18`).
- **Zona horaria**: `DateTime.UtcNow.AddHours(-5)` en **28 puntos** del backend (repo, auth, correlativos, caja…).
- **Cultura**: `es-PE` única (`Program.cs:78-81,191-197`); sin i18n multi-idioma ni formato de dinero multi-cultura en frontend.
- **Documentos**: solo valida RUC/DNI (PER). No hay modelo de tipos de documento por país (SAT/NIT, DIAN/NIT, SRI/RUC, etc.).
- **UBIGEO**: esquema Peruano (`Ubigeo`), sin jerarquía país/región genérica.

#### H5. Facturación electrónica atada a un único proveedor/país
- `FacturacionProxy` (apisperu) es el único adaptador; `InvoiceJob` arma UBL 2.1 SUNAT directamente. No hay **interfaz genérica de emisión fiscal** (Strategy) que permita SAT-MX, DIAN-CO, SRI-EC, CPE-BO.

#### H6. Rubros espá/nightlife acoplados (bloquea multi-rubro)
- `anfitrionas` + `renta de cuarto` + `asistencia por pisos` (payloads, interfaces, vistas y slices de Redux) son propios de spa nostálgico.
- **Módulo roto (deuda)**: el frontend consume `/renta/*` y `/facturacion/listar-fichas` (`asistencia.reducer.ts`) que **no tienen controlador** — solo existe `IRentaCuartoRepositorio` huérfana.
- `Producto.RestriccionEdad` (18+) y "ticket interno" (serie "TIN") son rasgos del rubro actual.

#### H7. Endpoints desalineados frontend↔backend
- No existen: `/extensiones/rubros`, `/extensiones/sucursales`, `/renta/*`, `/facturacion/listar-fichas`.
- `productos.reducer.ts:27` consume `listar/productos` (sin `/`) mientras el backend expone `api/productos/listar` → error 404 latente en la búsqueda de la terminal POS.
- Ubigeos se traen de un host externo hardcodeado (`https://api.4devscorp.com/main/extensiones/ubigeos`, `extensiones..reducer.ts:129`).

#### H8. Registros DI incompletos/inconsistentes (bloquea extensibilidad)
- `Infrastructure/DependencyInjection.cs`: `CargaInicial`, `Dashboard`, `Caja` (servicio/repo) y `Category` **no están registrados**; mezcla `AddTransient`/`AddScoped` sin criterio; `FacturacionProxy` se registra 2 veces (`AddHttpClient` + `AddScoped`).

#### H9. Secreto/sensible en el repo (riesgo operativo multi-cliente)
- Claves JWT (`appsettings.json` + `JwtHelper.cs`), connection string con password, token apisper en código. `JwtHelper.cs` además está **muerto** (llave e issuer distintos a los usados) y es un foco de confusión.

#### H10. Sin pruebas ni automatización de onboarding
- No hay tests unitarios/integración; el alta de un nuevo tenant/esquema es manual; varios journey críticos (facturación SUNAT, caja) no tienen regresión.

---

## B. Modelo de datos objetivo

### B.1 Jerarquía (dimensiones)

```
PAIS (codigo, nombre, moneda, cultura, zonaHoraria, esquemaFiscal)
 ├── TENANT / Empresa (razón social, RUC/NIT generico, rubro, paisId, monedaId)
 │     ├── RUBRO (codigo: drogueria|barberia|spa|consultorio|tienda|…)
 │     ├── SUCURSAL / Sede (tenantId, pais?, ubigeo/ubicacion, moneda, activo)
 │     ├── CONFIG_FISCAL (por empresa+pais: serie, adaptador, token, credenciales)
 │     ├── USUARIO-Sucursal (pertenencia y rol por sede)
 │     └── MODULOS_RUBRO (RubroId, CodigoModulo, Activo)
 └── Catalogo maestro (Idioma, Moneda, Impuesto)
```

### B.2 Tablas nuevas (migración)

| Tabla | Claves | Notas |
|---|---|---|
| `Pais` | Id, Codigo(ISO-3166), Nombre, Idioma, MonedaCodigo, TimeZone, EsquemaFiscal | catálogo maestro |
| `Moneda` | Id, Codigo, Simbolo, Locale | para formateo |
| `Impuesto` | Id, PaisId, Nombre, Porcentaje, AplicableA(tipoDoc) | reemplaza el 18% fijo |
| `Sucursal` | Id, TenantId, Nombre, Direccion, UbigeoId/Geolocalizacion, MonedaId, Estado | la **sede** |
| `ConfiguracionFiscal` | Id, EmpresaId, PaisId, RUC/NIT, RazonSocial, NombreComercial, Direccion, SerieFactura, SerieBoleta/Comprobante, CodigoAdaptador, Token, Activo | fin de los hardcodes |
| `RubroModulo` | RubroId, CodigoModulo, Activo | feature flags por rubro |
| `Turnos` / `Cita` *(opcional)* | ProfesionalId, FechaHora, PacienteId, Estado, SedeId, ServicioId | para consultorios/kinesiología |
| `Servicio` (catálogo de servicios) | SedeId, Nombre, Precio, DuracionMin, Profesional | permite rubros de servicio sin "producto" |

### B.3 Alteraciones críticas

- Añadir **`SucursalId`** a: `Producto`, `Categoria`, `Grupo`, `Cliente`, `ComprobanteCabecera`, `Caja`, `Retiros`, `Pago`, `Usuario×Sucursal`. (Mantener `TenantId` global; sede = partición dentro del tenant.)
- **`Seriecorrelativo`**: agregar `SucursalId` + `TipoDocumentoVentaId` y guardar serie real SUNAT `B001/F001/RC07` (hoy `FAC/BOL/TIN` genérico y el job la concatena mal → `"FFAC"`).
- `User`: relación many-to-many **Tenant×Sucursal** para autorizar vendedores solo a ciertas sedes.
- `Producto`: opcional `EsServicio`, `DuracionMin`, `ProfesionalIdCross`.

---

## C. Arquitectura objetivo — Backend

### C.1 Estrategia de aislamiento de datos (decisión)

- **Fase inicial (recomendada): shared schema + `TenantId` + `SucursalId` con `HasQueryFilter`** (ya existe el mecanismo en `SpaContext`). Es suficiente para docenas de sedes/empresas, tiene costo operativo bajo y permite migración incremental.
- **Escala alta (>100 tenants / volúmenes grandes / SLA): schema-per-tenant o DB-per-tenant** con el **catálogo de conexiones en registry** (`TenantRegistry` ya es el punto de extensión). Requiere: `TenantRegistry.GetTenants()` poblado por tabla `Tenant.ConnectionString`, resolución por subdominio en `TenantResolver` (ya escrita), y crear `SpaContext` con la cadena dinámica (ya soportado en ctor).

Recomendación técnica: implementar *discriminator dinámico* vía middleware que inyecte el `TenantContext` (tenant+sede+país+moneda) en un `AsyncLocal`, leído por `SpaContext` y por los servicios (patrón `ITenantResolver` extendido).

### C.2 Resolución por request (`TenantContext`)

Nuevo `ITenantContext` cargado en un middleware:

```
Headers/Subdominio + JWT claims
   → Tenant (empresa, rubro, pais)
   → Sucursal activa (header X-Sucursal o claim)
   → Pais/Cultura/Moneda
   → ConfiguracionFiscal
   → ConnectionString (si DB-per-tenant)
   → Inyectado en SpaContext (query filters) y en servicios/strategies
```

### C.3 Facturación fiscal multi-país (Strategy + Factory)

1. Interfaz de dominio `IFiscalDocumentEmitter<Pais>`:
   `EmitInvoice()`, `EmitVoided()`, `EmitCreditNote()`, `GeneratePdf()`.
2. Implementaciones por país/proveedor: `SUNAT_ApiPeru`, `SAT_Mexico`, `DIAN_Colombia`, `SRI_Ecuador`, … registradas bajo política (`GetFiscalEmitter(pais)` vía `IFiscalEmitterFactory`).
3. `ConfiguracionFiscal` aporta token/serie/credenciales; el job de facturación itera por tenant/pais (`foreach tenant in registry → job por adaptador`).
4. Reemplazar `FacturacionProxy` (token en código) por clientes tipados registrados en DI que leen de `ConfiguracionFiscal`.

### C.4 Impuestos y moneda (no más `1.18` ni `PEN`)

- Mover cálculo IGV/IVA/ISV a `TaxCalculatorFactory` (Strategy por país) que consume `Impuesto`.
- `TipoCambio`/`Moneda` por sede; los totales se almacenan en `MonedaBase` + `MonedaLocal` y el tipo de cambio.
- Reemplazar los **28** `AddHours(-5)` por `TimeZoneInfo` del país (`TenantContext.ZonaHoraria`) vía helper `DateTimeHelper.Now(pais)`.

### C.5 Sede en el dominio

- Todo repositorio filtra por `TenantId` (global) + `SucursalId` (bajo demanda del claim).
- Caja: apertura/cierre/retiros por `SucursalId`; reportes por sede o consolidado.
- Inventario por sede (transferencia de stock entre sedes como feature transversal).

### C.6 RBAC por rubro/módulo

- `RubroModulo` define qué muestran `Module/Submodule` en la UI; `AspNetUserSubModule` ya controla permisos por usuario.
- Mantener el claim `rutas` (ya existente) y añadir claims `sede`, `pais`, `moneda`, `idioma`.

### C.7 Limpieza previa (habilitadores técnicos)

- **DI**: registrar todos los servicios con política uniforme (`AddScoped` para repos/servicios con estado por request, `AddTransient` solo para sin estado), incluir `CargaInicial`, `Dashboard`, `Categoria`, `Caja`, y desduplicar `FacturacionProxy`.
- **Series**: corregir formato en `ComprobanteRepository.ArmarInvoice` (`"F" + Serie` → serie declarada en `ConfiguracionFiscal`).
- **Eliminar** `JwtHelper.cs` muerto; mover secretos a user-secrets/env variables de despliegue.
- **Migración de datos**: backfill `TenantId` existente → crear tenant `SPASOLIS1`/empresa como datos semilla de un rubro nuevo (no código).

---

## D. Arquitectura objetivo — Frontend

### D.1 Configuración por entorno
- `VITE_API_URL`, `VITE_TENANT_DOMAIN`, `VITE_I18N`; reemplazar `baseUrl` fijo (`utils/axios.ts:8`) y el ubigeo hardcodeado.
- Interceptor axios ya agrega token; añadir headers `X-Sucursal` y `Accept-Language`.

### D.2 Modularidad por rubro (dynamic feature tree)
- `MData.ts` (subir a config del tenant: `/tenant/recursos` → menú + branding por rubro/empresa).
- `Dashboard.tsx` (rutas) generadas desde el payload de módulos; cargando vistas con `React.lazy`.
- Módulos opcionales (asistencia/anfitrionas/renta, turnos para consultorio, kardex droguería) como carpetas aisladas con su propio slice → se registran si `RubroModulo.Activo`.

### D.3 i18n y dinero
- `i18next` + `react-i18next` con recursos por idioma; `<Money>` component usando `Intl.NumberFormat(locale, {style:'currency', currency})`.
- Fechas con `moment`/`date-fns` usando zona horaria del tenant.
- Máscaras/validación de documentos parametrizadas por país (DNI/RUC vs NIT vs CI).

### D.4 Branding
- Nombre/logo/portada/fuentes ya existen en `Empresa` (`ImagenPortada`, `GifCarga`, `LogoSidebar`, `Logo`); exponerlos y sustituir `title = { name: "Spa" }`.

---

## E. Seguridad

1. **Claims (JWT)**: `tenant`, `sede`, `pais`, `empresa`, `rubro`, `moneda`, `idioma`, `rutas`.
2. **Autorización**: `[Authorize]` + `[Policy("SedeActive")]` + verificación de pertenencia `usuario×sede` en cada mutación.
3. **Filtros EF**: `HasQueryFilter(TenantId)` + filtro de sede en repos de venta (nunca confiar solo en el frontend).
4. **Secretos**: mover JWT secret, connection strings, token fiscal a secrets/env; nunca al repo.
5. **Onboarding seguro**: alta de tenant/sede/empresa solo vía flujo admin autenticado (hoy `TenantController` es `[AllowAnonymous]` en casi todo — cerrar).

---

## F. Plan de implementación

| Fase | Alcance | Criterios de aceptación |
|---|---|---|
| **F0. Línea base** (1 sem) | Migración limpia, `.env`, appsettings por entorno, cerrar `[AllowAnonymous]`, eliminar `JwtHelper` muerto, tests de humo `/health` | El repos debe instalar sin credenciales locales |
| **F1. Desacoplar empresa** (2–3 sem) | `ConfiguracionFiscal` + refactor `InvoiceJob`, `ComprobanteRepository.ArmarInvoice/GeneratePdfRequest`, `FacturacionProxy`; corregir serie; mover token a config | `grep -ri "10430936315\|SOLIS\|SPASOLIS1\|apisperu" Backend/src Frontend/src` sin datos de negocio |
| **F2. Multitenancy real** (2–3 sem) | Activar subdominio→tenant en `TenantResolver`; `TenantContext` middleware; backfill de datos; correlativos por tenant | 2 tenants operan en la misma BD sin cruzarse datos ni series |
| **F3. Multi-sede** (2–3 sem) | Entidad `Sucursal`, columna `SucursalId`, caja/reporte por sede, header `X-Sucursal`, permisos `usuario×sede` | Abrir caja en 2 sedes a la vez sin interferencia |
| **F4. Multi-país** (2–4 sem) | Catálogo `Pais/Moneda/Impuesto`; `TaxCalculatorFactory`; `IFiscalDocumentEmitter` (1º Perú, luego MX/CO/EC); timezone/cultura dinámicos; tipos de doc por país | Un tenant peruano y uno mexicano facturan electrónicamente con su propio adaptador/impuesto |
| **F5. Multi-rubro** (2–3 sem) | `RubroModulo` + `Servicio` + `Turnos` (opcional); menú y rutas dinámicas; aislar/remover módulo renta o completarlo | Rubro droguería (kardex), barbería (servicios), consultorio (turnos) operables solo con datos |
| **F6. Escala / calidad** (continuo) | Tests (xUnit) de facturación por país, integración EF, CI (GitHub Actions): build + test + lint frontend; observabilidad (Serilog), paginación/índices; opcional schema/DB-per-tenant | PRs con tests verdes; métricas por tenant en dashboards |

**Orden recomendado**: F0→F1 (mayor ROI, desbloquea cualquier cliente nuevo) → F2 → F3 → F4 → F5. Las fases son incrementales y compatibles con el esquema actual.

---

## G. Deuda técnica detectada (backlog priorizado)

| # | Deuda | Archivo(s) | Prioridad |
|---|---|---|---|
| D1 | Empresa/RUC/token hardcodeados (bloquea multi-empresa) | `InvoiceJob.cs`, `ComprobanteRepository.cs`, `FacturacionProxy.cs` | Alta |
| D2 | Subdominio/multi-DB desactivados | `TenantResolver.cs`, `TenantRegistry.cs`, `SpaContext.cs` | Alta |
| D3 | No existe Sede | modelo + repos + caja | Alta |
| D4 | Moneda/IGV/timezone/cultura fijos a Perú | 28× `AddHours(-5)`, `1.18`, `PEN`, `es-PE` | Alta |
| D5 | Módulo Renta/Asistencia sin backend (`/renta/*`, `listar-fichas`) | `asistencia.reducer.ts`, `IRentaCuartoRepositorio.cs` | Media |
| D6 | Endpoint roto `listar/productos` | `productos.reducer.ts:27` | Media |
| D7 | `/extensiones/rubros` y `/extensiones/sucursales` inexistentes | `myBusiness.reducer.ts`, `ExtensionesController.cs` | Media |
| D8 | DI incompleto/inconsistente; `FacturacionProxy` duplicado | `Infrastructure/DependencyInjection.cs` | Media |
| D9 | Secretos en repo; `JwtHelper` muerto con llave distinta | `appsettings.json`, `JwtHelper.cs`, `DatabaseHelper.cs` | Alta (seguridad) |
| D10 | `TenantController` sin auth | `TenantController.cs` | Alta (seguridad) |
| D11 | Sin tests ni CI | repo | Media |

---

## H. Checklist para ATERRIZAR un nuevo cliente/rubro

1. `Pais` + `Impuesto` + `Moneda` dados de alta en catálogo.
2. `Tenant + Empresa + Rubro`; `ConfiguracionFiscal` (RUC/NIT, series, token del adaptador).
3. `Sede(s)` creadas; usuario admin ligado a tenant×sede con `RubroModulo` habilitados.
4. `CargaInicialService.CrearDataInicialRubro(rubroId, tenantId)` (categorías/módulos por rubro).
5. Frontend desplegado con `VITE_API_URL` del tenant → renderiza branding + menú + dinero/idioma automáticamente.

**Verificación** (definición de "multi-{empresa,sede,país,rubro}"): dos tenants de países distintos facturan cada uno con su impuesto/adaptador, sedes sin pisarse cajas ni series, y un rubro nuevo entra **sin cambios de código**, únicamente datos + configuración.