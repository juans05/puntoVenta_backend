# Arquitectura Escalable Consolidada — Multi-Empresa · Multi-Sede · Multi-País · Multi-Rubro

> Documento único (fusión de `ARQUITECTURA_ESCALABLE_MULTI_EMPRESA_SEDE_PAIS_RUBRO.md` y
> `ESCALABILIDAD_MULTI_RUBRO.md`) para convertir el POS mono-negocio
> (**SOLIS SALON & SPA**, RUC 10430936315 — Perú) en una plataforma SaaS multi-tenant.
> Sustituye a ambos documentos; cualquier cambio futuro se hace aquí.

---

## 1. Principios de arquitectura

1. **Un código, muchos negocios**: cada tenant se define por datos, jamás por código (prohibido `if (tenant == "SPASOLIS1")`).
2. **Las empresas se configuran, no se programan**: RUC/NIT, razón social, series, token fiscal, impuesto y tiempo vienen de BD/`appsettings` por tenant+sede+país.
3. **Módulos por rubro (feature flags)**: `RubroModulo` declara qué módulos aplican; el backend expone menú y endpoints según rubro; el frontend renderiza solo lo habilitado.
4. **Aislamiento de datos por 3 ejes**: `TenantId` (empresa) → `SucursalId` (sede) → país (fiscal/impuesto/idioma). Correlativos, series, caja y comprobantes aislados por tenant+sede.
5. **Configuración externa**: URLs, CORS, tokens, secretos → env/`appsettings`/user-secrets; nunca en el repo.
6. **Escalar por módulos, no por copias**: nueva funcionalidad = módulo opcional, no fork.
7. **Multi-rubro/país deben entrar sin cambios de código**: solo datos + configuración.

---

## 2. Diagnóstico del estado actual

### 2.1 Stack

| Capa | Tecnología | Evidencia |
|---|---|---|
| Backend API | ASP.NET Core Web API + Kestrel `:5001` | `WEB_API/Program.cs` |
| Arquitectura | Clean-ish (Domain / Application / Infrastructure / WEB_API) | `Backend/WEB_API_SPA.sln` |
| ORM | EF Core + Npgsql (PostgreSQL), migraciones | `Infrastructure/Data/SpaContext.cs` |
| Auth | ASP.NET Identity + JWT Bearer (HS256) + refresh | `AuthenticationRepository.cs` |
| Jobs | Coravel (job SUNAT) | `WEB_API/Jobs/InvoiceJob.cs` |
| Frontend | React 18 + Vite + TS, Redux Toolkit, react-query, Tailwind, framer-motion | `Frontend/package.json` |
| Facturación electrónica | `apisperu.com` (UBL 2.1 SUNAT) | `Application/Proxies/FacturacionProxy.cs` |

### 2.2 Activos reutilizables (YA existe)

1. `EntityBase.TenantId` (`Domain/Entities/EntityBase.cs:15`) — toda entidad hereda `TenantId`, `UsuarioCreacion`, `FechaCreacion`, `Estado`.
2. Query filters EF: `HasQueryFilter(e => e.TenantId == _tenant.Name)` en ~17 entidades (`SpaContext.cs:55-75`).
3. Auto-asignación de tenant en `SaveChangesAsync` (`SpaContext.cs:108-125`).
4. `ITenantResolver`/`TenantRegistry` — detección de subdominio **escrita pero desactivada**.
5. Modelo OEM: `Tenant`, `Empresa`, `EmpresaTenant`, `Rubro` + `TenantController` y `CargaInicialController` (siembra por `RubroId`).
6. RBAC por módulos: `AspNetModule`, `AspNetSubModule`, `AspNetUserSubModule`, claim `rutas` en JWT.
7. `BaseService.SendAsync<T>` genérico (`Application/Services/BaseService/BaseService.cs`).
8. `Seriecorrelativo` con filtro de tenant (base para multi-serie).
9. Branding en `Empresa`: `Logo`, `LogoSidebar`, `ImagenPortada`, `GifCarga`.

### 2.3 Hallazgos críticos

#### H1. Empresa fiscal hardcodeada (bloquea multi-empresa)
- `InvoiceJob.cs:98-222` — RUC `10430936315`, "SOLIS EGUIZABAL VENTURA", dirección, tenant fijo `"SPASOLIS1"` (líneas 42, 64, 71, 111, 179).
- `ComprobanteRepository.cs:382-407, 468-541` — `GeneratePdfRequest()` filtra `TenantId == "SPASOLIS1"` y duplica la empresa.
- `FacturacionProxy.cs:29,39,50` — token apisperu **en el código fuente**.

#### H2. Multi-tenant no activado (bloquea multi-empresa/país)
- `TenantResolver.GetCurrentTenant()`: subdominio/registry **comentado**, siempre retorna `DefaultConnection`.
- `SpaContext` solo usa connection string si el resolver la entrega → 1 sola BD para todos.
- `DatabaseHelper.cs` tiene 2 connection strings hardcodeadas distintas.
- Correlativos de `Seriecorrelativo` no aislados por sucursal.

#### H3. No existe SEDE (bloquea multi-sede)
- Sin entidad `Sucursal` ni columna `SucursalId`. La caja opera a nivel tenant/usuario.
- Frontend llama `/extensiones/sucursales` (reducer `my-business`) pero **ese endpoint no existe**.

#### H4. País fijo a Perú (bloquea multi-país)
- Moneda `PEN`/`S/` hardcodeados en `InvoiceJob.cs:134` y toda la UI.
- IGV 18% en `ComprobanteRepository.cs:73-75,115` y `InvoiceJob.cs:150-157`.
- Zona horaria `DateTime.UtcNow.AddHours(-5)` en ~28 puntos (24 encontrados + helpers).
- Cultura `es-PE` única (`Program.cs:78-81,191-197`); sin i18n multi-idioma ni formato de dinero multi-cultura.
- Solo valida RUC/DNI (PER). Sin modelo de tipos de documento por país (SAT/NIT, DIAN/NIT, SRI/RUC…).
- `Ubigeo` esquema peruano, sin jerarquía país/región genérica.

#### H5. Facturación atada a un proveedor/país
- `FacturacionProxy` (apisperu) es el único adaptador; `InvoiceJob` arma UBL 2.1 SUNAT. Sin estrategia genérica por país.

#### H6. Rubros spa/nightlife acoplados (bloquea multi-rubro)
- `anfitrionas` + `renta de cuarto` + `asistencia por pisos` (payloads, vistas, slices) propios de spa nostálgico.
- **Módulo roto**: frontend consume `/renta/*` y `/facturacion/listar-fichas` (`asistencia.reducer.ts`) que **no tienen controlador** (solo `IRentaCuartoRepositorio` huérfana).
- `Producto.RestriccionEdad` (18+) y "ticket interno" (serie "TIN") rasgos del rubro actual.

#### H7. Endpoints desalineados frontend↔backend
- No existen: `/extensiones/rubros`, `/extensiones/sucursales`, `/renta/*`, `/facturacion/listar-fichas`.
- `productos.reducer.ts:27` consume `listar/productos` (sin `/`) vs backend `api/productos/listar` → 404 latente.
- Ubigeos desde host externo hardcodeado (`https://api.4devscorp.com/main/extensiones/ubigeos`).

#### H8. Registros DI incompletos/inconsistentes
- `Infrastructure/DependencyInjection.cs`: `CargaInicial`, `Dashboard`, `Caja`, `Category` sin registrar; mezcla `AddTransient`/`AddScoped`; `FacturacionProxy` registrado 2 veces.

#### H9. Secretos en el repo
- Claves JWT (`appsettings.json` + `JwtHelper.cs` muerto con llave distinta), connection string con password, token apisperu en código.

#### H10. Sin pruebas ni onboarding automatizado
- Sin tests unitarios/integración; alta de tenant/esquema manual; journeys críticos (SUNAT, caja) sin regresión.

---

## 3. Decisiones de diseño (acuerdos cerrados)

> Estas decisiones resuelven las contradicciones entre los dos documentos originales.

| # | Tema | Decisión |
|---|---|---|
| **D-A** | Tabla de configuración fiscal | **`ConfiguracionFiscal`** (nombre único; descarta `Configuracion`). Incluye `PaisId` (permite multi-país). El **impuesto NO es un campo IGV fijo** — se calcula por país vía `TaxCalculatorFactory` (ver D-D). |
| **D-B** | Jerarquía del rubro | `Rubro` es **catálogo global** (compartido entre tenants), NO hijo del tenant. `Tenant` referencia `RubroId`. Un rubro nuevo = dato, no código. |
| **D-C** | Módulo Renta/Asistencia/Anfitrionas | **Retirar del core** en F0/F1 (mover a feature flag deshabilitado por defecto). Si un cliente spa future lo pide, se completa como módulo opcional con su `RubroModulo`. No bloquea el roadmap. |
| **D-D** | Impuestos | `TaxCalculatorFactory` (Strategy por país) consume catálogo `Impuesto`. Se elimina `1.18`/`porcentajeIgv = 18` hardcodeado. |
| **D-E** | Aislamiento de datos | **Fase inicial: schema único + `TenantId` + `SucursalId` con `HasQueryFilter`** (ya existe el mecanismo). **Escala alta (>100 tenants/SLA/esquema propio): DB-per-tenant** vía `TenantRegistry.GetTenants()` + `Tenant.ConnectionString` + `SpaContext` dinámico (ya soportado en ctor). |
| **D-F** | Resolución por request | Middleware `TenantContext` en `AsyncLocal`: headers/subdominio + JWT claims → tenant → sucursal (`X-Sucursal`) → país/cultura/moneda → `ConfiguracionFiscal` → connection string opcional. |
| **D-G** | Estrategia fiscal | `IFiscalDocumentEmitter<Pais>` + `IFiscalEmitterFactory` (`SUNAT_ApiPeru` primero; luego `SAT_MX`, `DIAN_CO`, `SRI_EC`). `FacturacionProxy` reemplazado por clientes tipados que leen de `ConfiguracionFiscal`. |
| **D-H** | Zona horaria | Helper único `DateTimeHelper.Now(pais)` usando `TimeZoneInfo` desde catálogo `Pais`; reemplaza los ~28 `AddHours(-5)`. |
| **D-I** | Series | `Seriecorrelativo` guarda serie real (`F001/B001/RC01`); se corrige la concatenación `"F"+Serie → "FFAC"` en `ComprobanteRepository.ArmarInvoice`. |
| **D-J** | Secretos | Movidos a user-secrets/env del despliegue; se elimina `JwtHelper.cs` muerto; `TenantController` se cierra (fuera de `[AllowAnonymous]` salvo login). |

---

## 4. Modelo de datos objetivo

### 4.1 Jerarquía (dimensiones)

```
PAIS (codigo ISO-3166, nombre, idioma, monedaCodigo, zonaHoraria, esquemaFiscal)
 ├── RUBRO (catálogo GLOBAL: drogueria|barberia|spa|consultorio|tienda|…)
 │     └── RUBRO_MODULO (RubroId, CodigoModulo, Activo) — feature flags
 ├── TENANT / Empresa (razonSocial, docFiscal generico, rubroId, paisId, monedaId)
 │     ├── EMPRESA (logo/portada/fuentes/branding) ← puente EmpresaTenant
 │     ├── SUCURSAL / Sede (tenantId, nombre, direccion, ubigeo/geo, monedaId, activo)
 │     ├── CONFIG_FISCAL (empresaId, paisId, RUC/NIT, razonSocial, serieFac,
 │     │                 serieBol, codigoAdaptador, token, activo)
 │     ├── USUARIO×TENANT×SUCURSAL (pertenencia y rol por sede)
 │     └── MODULOS del tenant (heredados de RubroModulo)
 └── Catalogos maestro: `Pais`, `Moneda`, `Impuesto`, `Idioma`, `Ubigeo`(peruano) → `Region/PaisAdmin`
```

### 4.2 Tablas nuevas (migración)

| Tabla | Claves | Notas |
|---|---|---|
| `Pais` | Id, Codigo(ISO-3166), Nombre, Idioma, MonedaCodigo, TimeZone, EsquemaFiscal | catálogo maestro |
| `Moneda` | Id, Codigo, Simbolo, Locale | formateo con `Intl.NumberFormat` |
| `Impuesto` | Id, PaisId, Nombre, Porcentaje, AplicableA(tipoDoc) | reemplaza el 18% fijo |
| `Sucursal` | Id, TenantId, Nombre, Direccion, UbigeoId/Geolocalizacion, MonedaId, Estado | la **sede** |
| `ConfiguracionFiscal` | Id, EmpresaId, PaisId, RUC/NIT, RazonSocial, NombreComercial, Direccion, SerieFactura, SerieBoleta, SerieNota, CodigoAdaptador, Token, Activo | fin de los hardcodes |
| `RubroModulo` | RubroId, CodigoModulo, Activo | feature flags por rubro |
| `Servicio` *(opcional)* | SedeId, Nombre, Precio, DuracionMin, Profesional | rubros de servicio sin "producto" |
| `Turnos` / `Cita` *(opcional)* | ProfesionalId, FechaHora, PacienteId, Estado, SedeId, ServicioId | consultorios/kinesiología |

### 4.3 Alteraciones críticas

- Añadir **`SucursalId`** a: `Producto`, `Categoria`, `Grupo`, `Cliente`, `ComprobanteCabecera`, `Caja`, `Retiros`, `Pago`, `Usuario×Sucursal`. (`TenantId` global; sede = partición dentro del tenant.)
- **`Seriecorrelativo`**: agregar `SucursalId` + `TipoDocumentoVentaId`; guardar serie real (`F001/B001/RC01`).
- `User`: relación many-to-many **Tenant×Sucursal** (vendedor autorizado solo a ciertas sedes).
- `Producto`: opcional `EsServicio`, `DuracionMin`, `ProfesionalIdCross`.
- Remplazar tabla `Ubigeo` por jerarquía genérica `Region` (país/región/ciudad) con compatibilidad hacia el esquema peruano.

---

## 5. Arquitectura objetivo — Backend

### 5.1 Resolución por request (`TenantContext`)

```
Headers/Subdominio + JWT claims
   → Tenant (empresa, rubro, pais)
   → Sucursal activa (header X-Sucursal o claim)
   → Pais/Cultura/Moneda
   → ConfiguracionFiscal
   → ConnectionString (solo en DB-per-tenant)
   → Inyectado en SpaContext (query filters) y en servicios/strategies
```

Implementación: middleware en el pipeline HTTP que resuelve e inyecta `ITenantContext` en un `AsyncLocal`, leído por `SpaContext` y por servicios (patrón `ITenantResolver` extendido).

### 5.2 Facturación fiscal multi-país

1. Interfaz de dominio `IFiscalDocumentEmitter<Pais>`: `EmitInvoice()`, `EmitVoided()`, `EmitCreditNote()`, `GeneratePdf()`.
2. Implementaciones: `SUNAT_ApiPeru` (1º), luego `SAT_Mexico`, `DIAN_Colombia`, `SRI_Ecuador`… registradas bajo `IFiscalEmitterFactory` (por política de país).
3. `ConfiguracionFiscal` aporta token/serie/credenciales. El job de facturación itera por tenant/pais (`foreach tenant in registry → job por adaptador`).
4. `FacturacionProxy` (token en código) se reemplaza por clientes tipados en DI que leen de `ConfiguracionFiscal`.

### 5.3 Impuestos y moneda

- `TaxCalculatorFactory` (Strategy por país) consume `Impuesto`.
- `TipoCambio`/`Moneda` por sede; totales almacenados en `MonedaBase` + `MonedaLocal` + tipo de cambio.
- Los ~28 `AddHours(-5)` → `DateTimeHelper.Now(pais)` con `TimeZoneInfo` del país.

### 5.4 Sede en el dominio

- Todo repositorio filtra `TenantId` (global) + `SucursalId` (bajo claim).
- Caja: apertura/cierre/retiros por `SucursalId`; reportes por sede o consolidado.
- Inventario por sede; transferencia de stock entre sedes como feature transversal.

### 5.5 RBAC por rubro/módulo

- `RubroModulo` define qué `Module/Submodule` se muestran; `AspNetUserSubModule` controla permisos por usuario.
- Claims JWT: `rutas` (ya existe) + `sede`, `pais`, `moneda`, `idioma`.

### 5.6 Higiene de registros (DI)

- Póliza uniforme: `AddScoped` para repos/servicios con estado por request; `AddTransient` solo sin estado.
- Registrar `CargaInicial`, `Dashboard`, `Categoria`, `Caja`; desduplicar `FacturacionProxy` (1 solo registro).
- Migración de datos: backfill `TenantId` → crear tenant `SPASOLIS1`/empresa como datos semilla (no código).

---

## 6. Arquitectura objetivo — Frontend

### 6.1 Configuración por entorno
- `VITE_API_URL`, `VITE_TENANT_DOMAIN`, `VITE_I18N`; reemplazar `baseUrl` fijo (`utils/axios.ts:8`) y ubigeo hardcodeado (`extensiones..reducer.ts:129` → `api/extensiones/ubigeos`).
- Interceptor axios ya agrega token → añadir headers `X-Sucursal` y `Accept-Language`.

### 6.2 Modularidad por rubro (dynamic feature tree)
- `MData.ts` → menú desde payload `/tenant/recursos` (branding + módulos del tenant/rubro).
- `Dashboard.tsx` (rutas) generadas desde el payload de módulos; vistas con `React.lazy`.
- Módulos opcionales (asistencia/renta, turnos, kardex droguería) en carpetas aisladas con su propio slice → se registran si `RubroModulo.Activo`.

### 6.3 i18n y dinero
- `i18next` + `react-i18next`; `<Money>` con `Intl.NumberFormat(locale, {style:'currency', currency})`.
- Fechas con `moment`/`date-fns` en zona horaria del tenant.
- Máscaras/validación de documentos por país (DNI/RUC vs NIT vs CI).

### 6.4 Branding
- Nombre/logo/portada/fuentes desde `Empresa` (campos ya existen); sustituir `title = { name: "Spa" }` de `MData.ts`.

### 6.5 Reglas de negocio por rubro (configurables)
- El "red-hotels" de `Facturacion/index.tsx` (redirect de RECEPCION/CONTADORA) → regla por rubro (p. ej. "vendedor con banca accede a ventas").

---

## 7. Seguridad

1. **Claims (JWT)**: `tenant`, `sede`, `pais`, `empresa`, `rubro`, `moneda`, `idioma`, `rutas`.
2. **Autorización**: `[Authorize]` + `[Policy("SedeActive")]` + verificación de pertenencia `usuario×sede` en cada mutación (incluida caja/venta, no solo controladores).
3. **Filtros EF**: `HasQueryFilter(TenantId)` + filtro de sede en repos de venta (nunca confiar solo en el frontend).
4. **Secretos**: JWT secret, connection strings, token fiscal → user-secrets/env; nunca al repo.
5. **Onboarding seguro**: alta de tenant/sede/empresa solo vía flujo admin autenticado. `TenantController` hoy `[AllowAnonymous]` en casi todo → cerrar (salvo login).

---

## 8. Plan de implementación por fases

| Fase | Alcance | Criterios de aceptación |
|---|---|---|
| **F0. Línea base** (1 sem) | Migración limpia; `.env` y `appsettings.*` por entorno; cerrar `[AllowAnonymous]`; eliminar `JwtHelper.cs` muerto; tests de humo `/health`; retirar módulo renta/asistencia del core (feature flag off) | El repo instala sin credenciales locales; no hay rutas `/renta/*` en el core |
| **F1. Desacoplar empresa** (2–3 sem) | `ConfiguracionFiscal` + refactor `InvoiceJob`, `ComprobanteRepository.ArmarInvoice/GeneratePdfRequest`, `FacturacionProxy`; corregir serie; token a config; `TaxCalculatorFactory` inicial (Perú) | `grep -ri "10430936315\|SOLIS\|SPASOLIS1\|apisperu" Backend Frontend/src` sin datos de negocio |
| **F2. Multi-tenant real** (2–3 sem) | Activar subdominio→tenant en `TenantResolver`; middleware `TenantContext`; backfill de datos; correlativos por tenant | 2 tenants en la misma BD sin cruzarse datos ni series |
| **F3. Multi-sede** (2–3 sem) | Entidad `Sucursal`, columna `SucursalId`, caja/reporte por sede, header `X-Sucursal`, permisos `usuario×sede` | Abrir caja en 2 sedes a la vez sin interferencia |
| **F4. Multi-país** (2–4 sem) | Catálogo `Pais/Moneda/Impuesto`; `TaxCalculatorFactory`; `IFiscalDocumentEmitter` (1º Perú, luego MX/CO/EC); timezone/cultura dinámicos; tipos de doc por país | Tenant peruano y uno mexicano facturan con su propio adaptador/impuesto |
| **F5. Multi-rubro** (2–3 sem) | `RubroModulo` + `Servicio` + `Turnos` (opcional); menú y rutas dinámicas; `CargaInicial` por rubro | Rubro droguería, barbería, consultorio operables SOLO con datos + flags |
| **F6. Escala / calidad** (continuo) | Tests xUnit de facturación por país; integración EF; CI (GitHub Actions) build+test+lint frontend; Serilog; paginación/índices; **migración a DB-per-tenant si se excede el umbral** | PRs con tests verdes; métricas por tenant |

**Orden recomendado**: F0 → F1 (mayor ROI, desbloquea cualquier cliente) → F2 → F3 → F4 → F5. Fases incrementales y compatibles con el esquema actual.

---

## 9. Checklist para aterrizar un nuevo cliente/rubro

1. `Pais` + `Impuesto` + `Moneda` dados de alta en catálogo (si es país nuevo).
2. `Rubro` (o reutilizar uno existente) + `RubroModulo` habilitados.
3. `Tenant` + `Empresa` (logos/branding) + `ConfiguracionFiscal` (RUC/NIT, series, token).
4. `Sede(s)` creadas; usuario admin ligado a tenant×sede.
5. `CargaInicialService.CrearDataInicialRubro(rubroId, tenantId)` (categorías/módulos por rubro).
6. Frontend desplegado con `VITE_API_URL` del tenant → renderiza branding + menú + dinero/idioma automáticamente.

**Verificación ("multi-{empresa,sede,país,rubro}")**: dos tenants de países distintos facturan cada uno con su impuesto/adaptador; sedes sin pisarse cajas ni series; un rubro nuevo entra **sin cambios de código**, únicamente datos + configuración.

---

## 10. Deuda técnica priorizada (backlog)

| # | Deuda | Archivo(s) | Prioridad |
|---|---|---|---|
| D1 | Empresa/RUC/token hardcodeados | `InvoiceJob.cs`, `ComprobanteRepository.cs`, `FacturacionProxy.cs` | Alta |
| D2 | Subdominio/multi-DB desactivados | `TenantResolver.cs`, `TenantRegistry.cs`, `SpaContext.cs` | Alta |
| D3 | No existe Sede | modelo + repos + caja | Alta |
| D4 | Moneda/IGV/timezone/cultura fijos a Perú | ~28× `AddHours(-5)`, `1.18`, `PEN`, `es-PE` | Alta |
| D5 | Módulo Renta/Asistencia sin backend (`/renta/*`, `listar-fichas`) | `asistencia.reducer.ts`, `IRentaCuartoRepositorio.cs` | Media (retirar) |
| D6 | Endpoint roto `listar/productos` | `productos.reducer.ts:27` | Media |
| D7 | `/extensiones/rubros` y `/extensiones/sucursales` inexistentes | `myBusiness.reducer.ts`, `ExtensionesController.cs` | Media |
| D8 | DI incompleto; `FacturacionProxy` duplicado | `Infrastructure/DependencyInjection.cs` | Media |
| D9 | Secretos en repo; `JwtHelper` muerto | `appsettings.json`, `JwtHelper.cs`, `DatabaseHelper.cs` | Alta |
| D10 | `TenantController` sin auth | `TenantController.cs` | Alta |
| D11 | URL externa hardcodeada de ubigeos | `extensiones..reducer.ts:129` | Media |
| D12 | Sin tests ni CI | repo | Media |

---

## 11. Métricas de éxito

- Un rubro nuevo (p. ej. retail) factura en producción **sin cambios de código** (solo datos/config).
- Dos tenants comparten BD sin pisarse series, clientes ni catálogos.
- La configuración fiscal se administra desde UI ("Mi empresa"), no en el repositorio.
- Nuevo módulo = registro de `RubroModulo` + vista opcional (no tocar el flujo de venta core).
- Dos sedes operan cajas abiertas de forma simultánea sin interferencia.
- Dos países facturan electrónicamente con su propio adaptador, impuesto y cultura.