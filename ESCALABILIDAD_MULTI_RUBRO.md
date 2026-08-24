# Plan de Escalabilidad Multi-Tenant / Multi-Rubro

> **Objetivo**: convertir el POS de un mono-negocio (SOLIS SALON & SPA) en una plataforma SaaS multi-tenant que sirva a **cualquier rubro** (retail, restaurantes, servicios, etc.) donde cada cliente (tenant) tenga su propia empresa, catálogo, series/correlativos, imágenes y módulos habilitados, manteniendo un único código base escalable.

---

## 1. Diagnóstico actual

El sistema **ya está pensado para multi-tenant/multi-rubro** a nivel de modelo de datos, pero la implementación lo contradice:

| Aspecto | Estado actual | Problema |
|---|---|---|
| `Tenant`, `Rubro`, `Empresa`, `EmpresaTenant` | Existen en BD | Solo hay 1 tenant en uso ("SPASOLIS1") |
| `TenantResolver` / `TenantRegistry` | Escritos pero no activos | Siempre devuelve `DefaultConnection` (1 sola BD para todos) |
| Empresa fiscal (RUC, razón social, dirección) | **Hardcodeada** en `InvoiceJob`, `ComprobanteRepository` | Otro negocio no puede facturar |
| Token de facturación (apisperu) | **Hardcodeado** en `FacturacionProxy` | No configurable por tenant |
| Series/correlativos | Globales (tabla `Seriecorrelativo`) | 2 empresas compartirían serie |
| Anfitrionas / Renta de cuarto / Asistencia por pisos | Código presente | Específico de spa/nightlife |
| Módulo `Asistencia` (frontend → `/renta/*`) | Llama a endpoints **que no existen** en el backend | Módulo roto / sin implementar |
| URLs externas | Incrustadas en frontend (`4devscorp.com`, `localhost`, subdominio fijo `spasolis`) | No desplazable |

---

## 2. Principios de arquitectura objetivo

1. **Un código, muchos negocios**: cada tenant se define por datos, no por código (nada de `if (tenant == "SPASOLIS")`).
2. **Las empresas se configuran, no se programan**: RUC, razón social, domicilio, series y token de facturación vienen de la BD/`appsettings` por tenant.
3. **Módulos por rubro (feature flags)**: cada `Rubro` declara qué módulos aplica (pos, categorias, asistencia, anfitrionas, reportes…). El backend expone menú y endpoints según rubro; el frontend renderiza solo lo habilitado.
4. **Aislamiento de datos**: al menos correlativos, series, comprobantes y config fiscal por tenant. Idealmente `connection string` por tenant (multi-base) o `TenantId` global con query filters en multi-esquema.
5. **Configuración externa**: URLs, CORS, tokens, directorios → `appsettings`, variables de entorno, secrets.
6. **Escalar por módulos, no por copias**: nueva funcionalidad = módulo opcional, no fork.

---

## 3. Diseño propuesto

### 3.1 Aislamiento de tenant

**Opción A (recomendada al inicio): BD única + `TenantId` en cada fila.**
- [ ] Activar `ITenantResolver.GetCurrentTenant()` real: subdominio → `Tenant` (ya hay esqueleto en `TenantResolver.cs` y `TenantRegistry`).
- [ ] Inyectar el tenant resuelto al `SpaContext` (filtro global `HasQueryFilter(x => x.TenantId == tenant)`).
- [ ] Agregar `TenantId` a: `Producto`, `Categoria`, `Grupo`, `Proveedor`, `Cliente`, `ComprobanteCabecera`, `Pago`, `Caja`, `Seriecorrelativo`, `Metodopago`, `User`.
- [ ] `EmpresaTenant` como puente: un tenant puede tener N sucursales/empresas.

**Opción B (a futuro, cuando la escala lo exija): 1 base por tenant.**
- Mantener el `DefaultConnection` para "ADMIN" (catálogo de tenants/empresas) y una tabla `Tenant.ConnectionString`.
- `TenantResolver` devuelve esa conexión y se crea el `SpaContext` dinámico.

### 3.2 Configuración fiscal por tenant (fin de los hardcodes)

Crear una tabla `Configuracion` (EmpresaId, TipoDoc, RUC, RazonSocial, NombreComercial, Direccion, UbigeoId, SerieFac, SerieBol, SerieNota, ArchivoPfx, ClavePfx, TokenFacturacion, IGV).

| Archivo a cambiar | Qué se parametriza |
|---|---|
| `Backend WEB_API/Jobs/InvoiceJob.cs` | Empresa (company) y tenant leídos de `Empresa`/`Configuracion`; eliminar `"SPASOLIS1"` y el RUC/dirección. |
| `Backend Infrastructure/Repositories/ComprobanteRepository.cs` | `ArmarInvoice()` y `GeneratePdfRequest()` usan `Configuracion`; quitar `TenantId == "SPASOLIS1"`; corregir serie `"F" + Serie` → `"F001"` por tenant. |
| `Backend Application/Proxies/FacturacionProxy.cs` | `AccessToken` (y `BaseUrl`) desde `Configuracion`/`appsettings` por tenant; dejar de usar `IServiceCollection.AddHttpClient` única. |
| `Backend WEB_API/Program.cs` | Job por tenant (looping sobre tenants activos) y re-habilitar el schedule (`.EveryFiveMinutes()`). |

**Correlativos por tenant:**
- `Seriecorrelativo` debe incluir `TenantId` + `EmpresaId` y guardar la serie real SUNAT (`B001`, `F001`, `RC01`) en lugar de `FAC/BOL/TIN`.
- `CrearComprobante` actualmente genera las series en `ComprobanteRepository` (serie "FAC"/"BOL"/"TIN"); delegar a `Configuracion`.

### 3.3 Módulos por rubro (feature flags)

**Backend:**
- [ ] Tabla/extensión de `Rubro` con lista de módulos: `RubroModulo (RubroId, CodigoModulo, Activo)`.
- [ ] `AspNetModule`/`AspNetSubModule` ya existen para permisos → vincular códigos de módulo-funcionalidad por rubro.
- [ ] `CargaInicialService.CrearDataInicialRubro(RubroId, TenantId)` (ya existe la interfaz) para sembrar categorías/recursos según rubro.
- [ ] Endpoint `/tenant/recursos` (ya existe) debe devolver **módulos habilitados + branding** del tenant, y el frontend lo consume para armar menú/rutas.
- [ ] Los módulos espá: `Renta` (`IRentaCuartoRepositorio`), `Anfitriona`, `Asistencia` → decidir **completarlos** (crear `RentaRepository`, servicio, `RentaController`) o **eliminarlos**; en ambos casos sin afectar el resto del código.

**Frontend:**
- [ ] `Dashboard.tsx` (routes) y `MData.ts` (menú) → construir de forma dinámica según los módulos que devuelve `/tenant/recursos`; hoy el menú es estático.
- [ ] `Sidebar/index.tsx` ya filtra por permisos del usuario (`me.rutas`), pero el menú base está hardcodeado en `MData.ts`.
- [ ] Quitar dependencia de módulos espá del core: `views/Admin/{Asistencia,reporte-asistencia}`, `Modal/Admin/{Asistencia,Anfitrionas,Fichas}`, `reducers/Admin/{asistencia,clientes-proveedores}`.

### 3.4 Branding y experiencia por tenant

- [ ] `MData.ts` (`title = { name: "Spa" }`) → nombre/logo/fuentes desde `Empresa` (ya existen campos `Logo`, `LogoSidebar`, `ImagenPortada`, `GifCarga`).
- [ ] Red-hotels: `Facturacion/index.tsx` redirige usuarios `RECEPCION/CONTADORA` — convertir en regla configurable por rubro (p. ej. "vendedor con banca accede a ventas").

### 3.5 Configuración y despliegue (CI/CD)

- [ ] `Frontend src/utils/axios.ts`: `baseUrl` por env (`VITE_API_URL`).
- [ ] `Frontend src/redux/reducers/extensiones/*`: ubigeos/tipo-doc/nacionalidad → use `baseUrl`, no URL externa fija.
- [ ] `Backend WEB_API/appsettings.json`: `DefaultConnection`, `ApiUrl`, tokens → mover secretos a secrets/config por entorno; pasar CORS y puertos (`Kestrel:ListenAnyIP(5001)`) a configuración.
- [ ] `Backend WEB_API/Program.cs`: CORS ("AllowAll") con orígenes por entorno; el `AddScoped<InvoiceJob>` y `scheduler.OnWorker("InvoiceJob")` → registrados por tenant.

### 3.6 Higiene de registros (DI) — habilitador de escalabilidad

- [ ] `Backend Infrastructure/DependencyInjection.cs` tiene registros incompletos/inconsistentes: `CargaInicial`, `Dashboard`, `Categoria`, `Caja`, `Renta` o no están o mezclan `AddTransient`/`AddScoped`. Unificar por ciclo de vida coherente y registrar nuevos servicios explícitamente.

---

## 4. Plan de implementación por fases

### Fase 0 — Línea base (1–2 semanas)
- Ejecutar migración limpia y verificar endpoints (`/health`, swagger).
- Documentar el flujo de facturación actual (ya mapeado).
- Configurar `.env.frontend` y `appsettings.*` por entorno.

### Fase 1 — Desacoplar la empresa de la facturación (2–3 semanas) — **mayor impacto**
- Crear entidad `Configuracion` + endpoint de gestión.
- Refactorizar `InvoiceJob`, `ComprobanteRepository.ArmarInvoice/GeneratePdfRequest`, `FacturacionProxy` para leer de `Configuracion`.
- Corregir formato de serie y soportar correlativos por tenant.
- **Resultado**: un segundo negocio puede facturar sin tocar código.

### Fase 2 — Multi-tenant real (2–3 semanas)
- Activar `TenantResolver` por subdominio + `TenantRegistry`.
- Agregar `TenantId` y query filters globales; migración de datos del tenant único.
- Por tenant: `Metodopago`, series, usuarios y parámetros.
- **Resultado**: N negocios operando con datos aislados.

### Fase 3 — Módulos por rubro (2–3 semanas)
- Catálogo `RubroModulo` + payload de módulos en `/tenant/recursos`.
- Menú dinámico y rutas dinámicas en frontend desde branding/módulos.
- Aislar (completar o retirar) `Renta/Anfitriona/Asistencia`.
- `CargaInicial` sembrando categorías/recursos por rubro.
- **Resultado**: un tenant de retail y otro de spa conviven con UIs distintas.

### Fase 4 — Escalabilidad vertical (continuo)
- 1 base por tenant (Opción B) cuando el volumen lo pida.
- Job de SUNAT por tenant con colas (Hangfire/BullMQ) y reintentos con backoff.
- Observabilidad: logging estructurado, métricas por tenant, tests (regresión de facturación por tipo de comprobante).
- Monorepo: separar frontend por paquetes (módulos) para despliegues selectivos.

---

## 5. Checklist por fichas de salida al mercado

Gracias a lo anterior, cada cliente nuevo solo requiere:

1. Crear `Tenant` + `Rubro` + `Empresa` (datos, logos).
2. Crear usuario admin y asignar módulos por `AspNetModule`/`RubroModulo`.
3. Cargar `Configuracion` fiscal (RUC, series, token de facturación).
4. Ejecutar `CargaInicialService.CrearDataInicialRubro` (categorías base según rubro).
5. Subir el frontend apuntando a su dominio/subdominio → auto-renderiza branding y menú.

**Autorrevisión antes de declarar "multi-rubro":** `grep -ri "SPASOLIS\|10430936315\|SOLIS\|apisperu\|4devscorp" Backend Frontend/src` no debe devolver datos de negocio (salvo config/seed renovada).

---

## 6. Métricas de éxito

- Un rubro nuevo (p. ej. retail) factura en producción **sin cambios de código** (solo datos/config).
- Dos tenants comparten BD sin pisarse series, clientes ni catálogos.
- La configuración fiscal del cliente se administra desde UI ("Mi empresa"), no en el repositorio.
- Nuevo módulo añadido = añadir registro de `RubroModulo` + vista opcional (no tocar flujo de venta core).