# Fecha de registro bloqueada + fecha de transacción corregible — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exponer "fecha de registro" (bloqueada) junto a la fecha real de la transacción en Gasto, Compra y Venta; permitir corregir esa fecha en un registro ya guardado mediante una acción de desbloqueo de emergencia; ocultar anulados en las búsquedas de Compras y Ventas.

**Architecture:** Backend en capas (Controller → Service → Repository, patrón EF Core + AutoMapper ya existente). Cada entidad (Gasto, Compra, Venta/ComprobanteCabecera) gana un endpoint `PUT` dedicado para corregir solo su fecha de transacción, siguiendo el patrón `{id}` en ruta + payload en el body que ya usa `CambiarEstadoCategoria` en `GastoController.cs:33-34`. Frontend: un componente modal compartido y sin lógica de API propia (`CorregirFechaModal`), reutilizado desde las tres pantallas de listado.

**Tech Stack:** .NET 8 / EF Core / AutoMapper / PostgreSQL (backend). React + Redux (createReducer/thunks) + react-modal + @iconify/react (frontend).

**Spec:** `docs/superpowers/specs/2026-08-26-fecha-registro-transaccion-design.md`

## Global Constraints

- `FechaGasto` y `FechaCompra` ya existen como columnas en la base de datos (migración `20260817025424_AddGestionModulos`) — no requieren migración nueva.
- `FechaVenta` no existe — requiere una migración EF nueva, columna **nullable**, sin backfill.
- La corrección de fecha de Venta es **solo de presentación**: nunca debe escribir en `FechaCreacion` ni en ninguna query de reporte/SUNAT existente.
- Ningún endpoint de corrección de fecha debe permitir modificar un registro `ANULADO` / `EstatusComprobante.Anulado`.
- El desbloqueo no requiere rol especial — cualquier usuario que ya accede a la pantalla puede desbloquear y corregir.
- `ListarCompras` y `ListarComprobantes` deben excluir anulados; `ListarGastos`, `ObtenerCompra`, `ListarComprobantesAnulados`, `ListarComprobantesPendientesEnviarSunat` **no se tocan**.
- **Este repositorio no tiene proyecto de tests (backend ni frontend).** No se crea infraestructura de testing nueva — cada tarea se verifica con `dotnet build` / `npm run build` + una verificación manual (curl o navegador), siguiendo la convención real del proyecto.
- `SpaContext.SaveChangesAsync` (`Infrastructure/Data/SpaContext.cs:172-226`) ya audita automáticamente cualquier cambio a una entidad `EntityBase` (incluye `Gasto`, `Compra`, `ComprobanteCabecera`) en la tabla `AuditLog` — las correcciones de fecha quedan con historial sin código adicional.
- **Pre-requisito antes de empezar:** el working tree tiene cambios sin commitear de un trabajo previo (ROI Publicidad) que tocan `Backend/Infrastructure/Repositories/ComprobanteRepository.cs` y `Backend/Infrastructure/Migrations/SpaContextModelSnapshot.cs` — dos archivos que Task 3 también modifica. Commitear (o stashear) ese trabajo previo antes de iniciar Task 3, para que los commits de esta feature no mezclen cambios de ROI Publicidad.

---

## Task 1: Backend — Gasto: exponer fecha de registro + endpoint de corrección

**Files:**
- Modify: `Backend/Domain/DTO/GastoDto.cs`
- Modify: `Backend/Domain/Common/Mappings/MyAutomapper.cs:123-126`
- Create: `Backend/Domain/Payloads/ActualizarFechaPayload.cs`
- Modify: `Backend/Application/Interfaces/IRepository/IGastoRepository.cs`
- Modify: `Backend/Infrastructure/Repositories/GastoRepository.cs`
- Modify: `Backend/Application/Interfaces/IServices/IGastoService.cs`
- Modify: `Backend/Application/Services/GastoService.cs`
- Modify: `Backend/WEB_API/Controllers/GastoController.cs`

**Interfaces:**
- Produces: `ActualizarFechaPayload { DateTime Fecha }` (reused by Task 2 and Task 3 — do not redefine it there).
- Produces: `PUT /api/gastos/modificar-fecha/{id}` — body `{ "fecha": "2026-08-20" }` — 200 on success, 400 if el gasto está `ANULADO`, 404 si no existe.
- Produces: `GastoDto.FechaRegistro` (string, formato `dd/MM/yyyy HH:mm:ss`) — consumido por el frontend en Task 5.

- [ ] **Step 1: Crear el payload compartido**

```csharp
// Backend/Domain/Payloads/ActualizarFechaPayload.cs
namespace Domain.Payloads;

public class ActualizarFechaPayload
{
    public DateTime Fecha { get; set; }
}
```

- [ ] **Step 2: Agregar `FechaRegistro` al DTO**

En `Backend/Domain/DTO/GastoDto.cs`, agregar el campo (después de `Estado`):

```csharp
    public string Estado { get; set; } = null!;
    public string FechaRegistro { get; set; } = null!;
    public string FechaGasto { get; set; } = null!;
```

- [ ] **Step 3: Mapear `FechaRegistro` en AutoMapper**

En `Backend/Domain/Common/Mappings/MyAutomapper.cs`, el bloque `CreateMap<Gasto, GastoDto>()` (línea 123) queda:

```csharp
            CreateMap<Gasto, GastoDto>()
                .ForMember(x => x.MetodoPago, y => y.MapFrom(z => z.Metodopago != null ? z.Metodopago.Descripcion ?? z.Metodopago.Nombre : null))
                .ForMember(x => x.FechaGasto, y => y.MapFrom(z => z.FechaGasto.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.FechaRegistro, y => y.MapFrom(z => z.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion));
```

- [ ] **Step 4: Agregar el método al repositorio**

En `Backend/Application/Interfaces/IRepository/IGastoRepository.cs`, agregar:

```csharp
    Task<(ServiceStatus, string)> ActualizarFechaGasto(int id, DateTime fecha);
```

En `Backend/Infrastructure/Repositories/GastoRepository.cs`, agregar (mismo patrón que `AnularGasto`, línea 74):

```csharp
    public async Task<(ServiceStatus, string)> ActualizarFechaGasto(int id, DateTime fecha)
    {
        var gasto = await _context.Gasto.AsTracking().FirstOrDefaultAsync(g => g.Id == id);

        if (gasto == null)
            return (ServiceStatus.NotFound, $"No se encontro el gasto {id}");

        if (gasto.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de un gasto anulado");

        gasto.FechaGasto = fecha;

        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, "Fecha actualizada correctamente");
    }
```

- [ ] **Step 5: Agregar el método al servicio**

En `Backend/Application/Interfaces/IServices/IGastoService.cs`, agregar:

```csharp
    Task<MessageResult<bool>> ActualizarFechaGasto(int id, DateTime fecha);
```

En `Backend/Application/Services/GastoService.cs`, agregar (mismo patrón que `CambiarEstadoCategoria`, línea 86):

```csharp
    public async Task<MessageResult<bool>> ActualizarFechaGasto(int id, DateTime fecha)
    {
        var (estado, message) = await _gastoRepository.ActualizarFechaGasto(id, fecha);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }
```

- [ ] **Step 6: Agregar el endpoint**

En `Backend/WEB_API/Controllers/GastoController.cs`, agregar junto a `AnularGasto`:

```csharp
    [HttpPut("modificar-fecha/{id}")]
    public async Task<IActionResult> ActualizarFechaGasto(int id, [FromBody] ActualizarFechaPayload payload) => Ok(await _gastoService.ActualizarFechaGasto(id, payload.Fecha));
```

- [ ] **Step 7: Compilar**

Run: `dotnet build` (desde `Backend/`)
Expected: build succeeds, 0 errores.

- [ ] **Step 8: Verificación manual**

Con la API corriendo localmente y un token válido:

```bash
curl -X PUT "http://localhost:PORT/api/gastos/modificar-fecha/1" \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"fecha":"2026-08-20T00:00:00"}'
```

Expected: `200 OK`. Repetir contra un gasto con `Estado == "ANULADO"` → `400 BadRequest` con el mensaje de validación. Repetir contra un id inexistente → `404 NotFound`.

- [ ] **Step 9: Commit**

```bash
git add Backend/Domain/DTO/GastoDto.cs Backend/Domain/Common/Mappings/MyAutomapper.cs Backend/Domain/Payloads/ActualizarFechaPayload.cs Backend/Application/Interfaces/IRepository/IGastoRepository.cs Backend/Infrastructure/Repositories/GastoRepository.cs Backend/Application/Interfaces/IServices/IGastoService.cs Backend/Application/Services/GastoService.cs Backend/WEB_API/Controllers/GastoController.cs
git commit -m "feat: expose fecha de registro and add fecha correction endpoint for Gasto"
```

---

## Task 2: Backend — Compra: fecha en creación + fecha de registro + endpoint de corrección + ocultar anulados

**Files:**
- Modify: `Backend/Domain/DTO/CompraDto.cs`
- Modify: `Backend/Domain/Payloads/CreateCompraPayload.cs`
- Modify: `Backend/Domain/Common/Mappings/MyAutomapper.cs:112-117`
- Modify: `Backend/Application/Interfaces/IRepository/ICompraRepository.cs`
- Modify: `Backend/Infrastructure/Repositories/CompraRepository.cs`
- Modify: `Backend/Application/Interfaces/IServices/ICompraService.cs`
- Modify: `Backend/Application/Services/CompraService.cs`
- Modify: `Backend/WEB_API/Controllers/CompraController.cs`

**Interfaces:**
- Consumes: `ActualizarFechaPayload` from Task 1 (`Backend/Domain/Payloads/ActualizarFechaPayload.cs`).
- Produces: `PUT /api/compras/modificar-fecha/{id}` — misma forma que Task 1.
- Produces: `CompraDto.FechaRegistro` (string) — consumido por el frontend en Task 6.
- Produces: `CreateCompraPayload.FechaCompra` (`DateTime?`, opcional) — consumido por el frontend en Task 6.

- [ ] **Step 1: Agregar `FechaCompra` opcional al payload de creación**

En `Backend/Domain/Payloads/CreateCompraPayload.cs`, agregar:

```csharp
    public DateTime? FechaCompra { get; set; }
```

- [ ] **Step 2: Usar la fecha del payload al crear**

En `Backend/Infrastructure/Repositories/CompraRepository.cs`, dentro de `CrearCompra` (línea ~63), cambiar:

```csharp
FechaCompra = NowLocal()
```

por:

```csharp
FechaCompra = payload.FechaCompra ?? NowLocal()
```

- [ ] **Step 3: Agregar `FechaRegistro` al DTO**

En `Backend/Domain/DTO/CompraDto.cs`, agregar (después de `Estado`):

```csharp
    public string Estado { get; set; } = null!;
    public string FechaRegistro { get; set; } = null!;
    public string FechaCompra { get; set; } = null!;
```

- [ ] **Step 4: Mapear `FechaRegistro` en AutoMapper**

En `Backend/Domain/Common/Mappings/MyAutomapper.cs`, el bloque `CreateMap<Compra, CompraDto>()` (línea 112) queda:

```csharp
            CreateMap<Compra, CompraDto>()
                .ForMember(x => x.Proveedor, y => y.MapFrom(z => z.Proveedor != null ? z.Proveedor.Nombre : null))
                .ForMember(x => x.MetodoPago, y => y.MapFrom(z => z.Metodopago != null ? z.Metodopago.Descripcion ?? z.Metodopago.Nombre : null))
                .ForMember(x => x.FechaCompra, y => y.MapFrom(z => z.FechaCompra.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.FechaRegistro, y => y.MapFrom(z => z.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion))
                .ForMember(x => x.Detalle, y => y.MapFrom(z => z.CompraDetalles));
```

- [ ] **Step 5: Ocultar anulados en `ListarCompras`**

En `Backend/Infrastructure/Repositories/CompraRepository.cs`, dentro de `ListarCompras` (línea ~180), cambiar:

```csharp
var query = _context.Compra.AsNoTracking().AsQueryable();
```

por:

```csharp
var query = _context.Compra.AsNoTracking().Where(c => c.Estado != "ANULADO").AsQueryable();
```

- [ ] **Step 6: Agregar el método de corrección de fecha al repositorio**

En `Backend/Application/Interfaces/IRepository/ICompraRepository.cs`, agregar:

```csharp
    Task<(ServiceStatus, string)> ActualizarFechaCompra(int id, DateTime fecha);
```

En `Backend/Infrastructure/Repositories/CompraRepository.cs`, agregar (mismo patrón que Task 1):

```csharp
    public async Task<(ServiceStatus, string)> ActualizarFechaCompra(int id, DateTime fecha)
    {
        var compra = await _context.Compra.AsTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            return (ServiceStatus.NotFound, $"No se encontro la compra {id}");

        if (compra.Estado == "ANULADO")
            return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de una compra anulada");

        compra.FechaCompra = fecha;

        await _context.SaveChangesAsync();

        return (ServiceStatus.Ok, "Fecha actualizada correctamente");
    }
```

- [ ] **Step 7: Agregar el método al servicio**

En `Backend/Application/Interfaces/IServices/ICompraService.cs`, agregar:

```csharp
    Task<MessageResult<bool>> ActualizarFechaCompra(int id, DateTime fecha);
```

En `Backend/Application/Services/CompraService.cs`, agregar:

```csharp
    public async Task<MessageResult<bool>> ActualizarFechaCompra(int id, DateTime fecha)
    {
        var (estado, message) = await _compraRepository.ActualizarFechaCompra(id, fecha);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }
```

- [ ] **Step 8: Agregar el endpoint**

En `Backend/WEB_API/Controllers/CompraController.cs`:

```csharp
    [HttpPut("modificar-fecha/{id}")]
    public async Task<IActionResult> ActualizarFechaCompra(int id, [FromBody] ActualizarFechaPayload payload) => Ok(await _compraService.ActualizarFechaCompra(id, payload.Fecha));
```

- [ ] **Step 9: Compilar**

Run: `dotnet build` (desde `Backend/`)
Expected: build succeeds, 0 errores.

- [ ] **Step 10: Verificación manual**

```bash
# Crear con fecha pasada
curl -X POST "http://localhost:PORT/api/compras/crear" -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"detalle":[{"productoId":1,"cantidad":1,"costoUnitario":10}],"fechaCompra":"2026-08-10T00:00:00"}'
# Verificar que la compra creada tiene fechaCompra = 10/08/2026, no hoy.

# Corregir fecha
curl -X PUT "http://localhost:PORT/api/compras/modificar-fecha/1" -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"fecha":"2026-08-20T00:00:00"}'
# Expected: 200 OK

# Listar y confirmar que no aparecen anulados
curl "http://localhost:PORT/api/compras/listar?Page=1&Amount=50" -H "Authorization: Bearer <token>"
# Expected: ningún item con estado "ANULADO"
```

- [ ] **Step 11: Commit**

```bash
git add Backend/Domain/DTO/CompraDto.cs Backend/Domain/Payloads/CreateCompraPayload.cs Backend/Domain/Common/Mappings/MyAutomapper.cs Backend/Application/Interfaces/IRepository/ICompraRepository.cs Backend/Infrastructure/Repositories/CompraRepository.cs Backend/Application/Interfaces/IServices/ICompraService.cs Backend/Application/Services/CompraService.cs Backend/WEB_API/Controllers/CompraController.cs
git commit -m "feat: settable fecha compra on create, fecha correction endpoint, hide anulados from listing"
```

---

## Task 3: Backend — Venta: nueva columna FechaVenta + endpoint de corrección + ocultar anulados

**Files:**
- Modify: `Backend/Domain/Entities/ComprobanteCabecera.cs`
- Create: migration via `dotnet ef migrations add`
- Modify: `Backend/Domain/DTO/ComprobanteCabeceraDto.cs`
- Modify: `Backend/Domain/Common/Mappings/MyAutomapper.cs:19-32`
- Modify: `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`
- Modify: `Backend/Application/Interfaces/IRepository/IComprobanteRepository.cs`
- Modify: `Backend/Application/Interfaces/IServices/IComprobanteService.cs`
- Modify: `Backend/Application/Services/ComprobanteService.cs`
- Modify: `Backend/WEB_API/Controllers/FacturacionController.cs`

**Interfaces:**
- Consumes: `ActualizarFechaPayload` from Task 1.
- Produces: `PUT /api/facturacion/modificar-fecha-venta/{id}` — misma forma que Task 1/2.
- Produces: `ComprobanteCabeceraDTO.FechaVenta` (string) — consumido por el frontend en Task 7.

- [ ] **Step 1: Agregar el campo a la entidad**

En `Backend/Domain/Entities/ComprobanteCabecera.cs`, agregar (después de `EstadoComprobante`):

```csharp
    public char EstadoComprobante { get; set; } = EstatusComprobante.Creado;
    public DateTime? FechaVenta { get; set; }
```

- [ ] **Step 2: Generar la migración**

Run (desde `Backend/`):
```bash
dotnet ef migrations add AddFechaVentaToComprobanteCabecera --project Infrastructure/Infrastructure.csproj --startup-project WEB_API/Spa.Api.csproj
```

Expected: se crean `Backend/Infrastructure/Migrations/<timestamp>_AddFechaVentaToComprobanteCabecera.cs` y su `.Designer.cs`, y `SpaContextModelSnapshot.cs` se actualiza. Abrir el `.cs` generado y confirmar que la columna es `nullable: true` (sin default).

- [ ] **Step 3: Aplicar la migración localmente**

Run:
```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project WEB_API/Spa.Api.csproj
```

Expected: comando termina sin error; la columna `FechaVenta` existe en la tabla de `ComprobanteCabecera`/`comprobante_cabecera` en la base local.

- [ ] **Step 4: Setear `FechaVenta` al crear el comprobante**

En `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`, dentro de `CrearComprobante`, justo después de `var cabecera = _mapper.Map<ComprobanteCabecera>(payload);` (línea 73), agregar:

```csharp
                var cabecera = _mapper.Map<ComprobanteCabecera>(payload);

                cabecera.FechaVenta = DateTime.UtcNow.AddHours(-5);
```

- [ ] **Step 5: Agregar `fechaVenta` al DTO**

En `Backend/Domain/DTO/ComprobanteCabeceraDto.cs`, agregar (después de `Fecha`):

```csharp
    public string Fecha { get; set; }
    public string FechaVenta { get; set; }
```

- [ ] **Step 6: Mapear `fechaVenta` en AutoMapper**

En `Backend/Domain/Common/Mappings/MyAutomapper.cs`, dentro del bloque `CreateMap<ComprobanteCabecera, ComprobanteCabeceraDTO>()` (línea 19), agregar antes del `;` de cierre:

```csharp
                .ForMember(dest => dest.Correlativo, opt => opt.MapFrom(src => src.Correlativo.ToString().PadLeft(7, '0')))
                .ForMember(dest => dest.Fecha, opt => opt.MapFrom(src => src.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(dest => dest.FechaVenta, opt => opt.MapFrom(src => (src.FechaVenta ?? src.FechaCreacion).ToString("dd/MM/yyyy HH:mm:ss")));
```

(Nota: el `.ForMember(... Fecha ...)` ya existe — solo se agrega la línea de `FechaVenta` a continuación, cambiando el `;` final de lugar.)

- [ ] **Step 7: Ocultar anulados en `ListarComprobantes`**

En `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`, dentro de `ListarComprobantes` (línea ~205), cambiar:

```csharp
            lista = await _context.ComprobanteCabecera.AsNoTracking()
                                                      .WhereIf(string.IsNullOrEmpty(queryparam.StartDate) && string.IsNullOrEmpty(queryparam.EndDate), s => s.FechaCreacion.Date >= DateTime.UtcNow.AddHours(-5).AddDays(-7).Date)
```

por:

```csharp
            lista = await _context.ComprobanteCabecera.AsNoTracking()
                                                      .Where(x => x.EstadoComprobante != EstatusComprobante.Anulado)
                                                      .WhereIf(string.IsNullOrEmpty(queryparam.StartDate) && string.IsNullOrEmpty(queryparam.EndDate), s => s.FechaCreacion.Date >= DateTime.UtcNow.AddHours(-5).AddDays(-7).Date)
```

- [ ] **Step 8: Agregar el método de corrección de fecha al repositorio**

En `Backend/Application/Interfaces/IRepository/IComprobanteRepository.cs`, agregar:

```csharp
    Task<(ServiceStatus, string)> ActualizarFechaVenta(int id, DateTime fecha);
```

En `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`, agregar (respetando la indentación de 8 espacios de esta clase):

```csharp
        public async Task<(ServiceStatus, string)> ActualizarFechaVenta(int id, DateTime fecha)
        {
            var comprobante = await _context.ComprobanteCabecera.AsTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (comprobante == null)
                return (ServiceStatus.NotFound, $"No se encontro el comprobante {id}");

            if (comprobante.EstadoComprobante == EstatusComprobante.Anulado)
                return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de un comprobante anulado");

            comprobante.FechaVenta = fecha;

            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Fecha de venta actualizada correctamente");
        }
```

- [ ] **Step 9: Agregar el método al servicio**

En `Backend/Application/Interfaces/IServices/IComprobanteService.cs`, agregar:

```csharp
    Task<MessageResult<bool>> ActualizarFechaVenta(int id, DateTime fecha);
```

En `Backend/Application/Services/ComprobanteService.cs`, agregar:

```csharp
    public async Task<MessageResult<bool>> ActualizarFechaVenta(int id, DateTime fecha)
    {
        var (estado, message) = await _comprobanteRepository.ActualizarFechaVenta(id, fecha);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }
```

- [ ] **Step 10: Agregar el endpoint**

En `Backend/WEB_API/Controllers/FacturacionController.cs`:

```csharp
    [HttpPut("modificar-fecha-venta/{id}")]
    public async Task<IActionResult> ActualizarFechaVenta(int id, [FromBody] ActualizarFechaPayload payload) => Ok(await _comprobanteService.ActualizarFechaVenta(id, payload.Fecha));
```

- [ ] **Step 11: Compilar**

Run: `dotnet build` (desde `Backend/`)
Expected: build succeeds, 0 errores.

- [ ] **Step 12: Verificación manual**

```bash
# Corregir fecha de venta
curl -X PUT "http://localhost:PORT/api/facturacion/modificar-fecha-venta/1" -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"fecha":"2026-08-20T00:00:00"}'
# Expected: 200 OK

# Confirmar que FechaCreacion no cambió y fechaVenta sí, en el listado
curl "http://localhost:PORT/api/facturacion/listar?Page=1&Amount=50" -H "Authorization: Bearer <token>"
# Expected: item con "fecha" (registro) igual a antes, "fechaVenta" = 20/08/2026, y ningún item con estadoComprobante "ANULADO"
```

- [ ] **Step 13: Commit**

```bash
git add Backend/Domain/Entities/ComprobanteCabecera.cs Backend/Infrastructure/Migrations/ Backend/Domain/DTO/ComprobanteCabeceraDto.cs Backend/Domain/Common/Mappings/MyAutomapper.cs Backend/Infrastructure/Repositories/ComprobanteRepository.cs Backend/Application/Interfaces/IRepository/IComprobanteRepository.cs Backend/Application/Interfaces/IServices/IComprobanteService.cs Backend/Application/Services/ComprobanteService.cs Backend/WEB_API/Controllers/FacturacionController.cs
git commit -m "feat: add FechaVenta column, fecha correction endpoint, hide anulados from ventas listing"
```

---

## Task 4: Frontend — Componente compartido CorregirFechaModal

**Files:**
- Create: `Frontend/src/components/Modal/Admin/CorregirFecha/index.tsx`
- Create: `Frontend/src/components/Modal/Admin/CorregirFecha/corregirFecha.module.css`

**Interfaces:**
- Produces: `<CorregirFechaModal isOpen fechaRegistro fechaActual onGuardar onClose />` — usado por Task 5, 6 y 7.
  - `fechaRegistro: string` — texto ya formateado (ej. `"20/08/2026 10:00:00"`), se muestra tal cual, sin lógica de fecha.
  - `fechaActual: string` — fecha de la transacción en formato `YYYY-MM-DD` (para el `<input type="date">`).
  - `onGuardar: (nuevaFecha: string) => Promise<void>` — recibe la fecha en formato `YYYY-MM-DD`.
  - `onClose: () => void`.
- Consumes: nada de tareas anteriores — este componente es puro/presentacional, sin llamadas a la API.

- [ ] **Step 1: Crear el CSS del modal**

Reutiliza exactamente las clases de `Frontend/src/components/Modal/Admin/Gasto/gasto.module.css` (overlay/panel/encabezado/section/buttons) y agrega las propias del candado:

```css
/* Frontend/src/components/Modal/Admin/CorregirFecha/corregirFecha.module.css */
.overlay {
  background-color: rgba(0, 0, 0, 0.5);
  bottom: 0;
  left: 0;
  right: 0;
  top: 0;
  position: fixed;
  z-index: 9999;
  display: flex;
  justify-content: center;
  align-items: center;
}

.panel {
  background: #fff;
  border-radius: 12px;
  max-width: 420px;
  width: 100%;
  padding: 24px;
  outline: none;
  position: relative;
}

.encabezado {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #f1f1f2;
  padding-bottom: 12px;
  margin-bottom: 16px;
}

.encabezado h3 {
  font-weight: 600;
  margin: 0;
  font-size: 16px;
}

.closeBtn {
  cursor: pointer;
  font-size: 18px;
  color: #475467;
  background: none;
  border: 0;
  padding: 4px;
}

.campo {
  margin-bottom: 16px;
}

.campo label {
  display: block;
  font-size: 12px;
  font-weight: 600;
  color: #99a1b7;
  text-transform: uppercase;
  margin-bottom: 6px;
}

.fechaRegistroValor {
  padding: 10px 12px;
  background: #f7f7f8;
  border-radius: 6px;
  font-size: 14px;
  color: #475467;
}

.fechaConCandado {
  display: flex;
  gap: 8px;
  align-items: center;
}

.fechaConCandado input {
  flex: 1;
  padding: 10px 12px;
  border: 1px solid #e6e5e5;
  border-radius: 6px;
  font-size: 14px;
}

.fechaConCandado input:disabled {
  background: #f7f7f8;
  color: #99a1b7;
}

.unlockBtn {
  border: 1px solid #2997FE;
  color: #2997FE;
  background: #fff;
  border-radius: 6px;
  padding: 9px 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
}

.aviso {
  font-size: 12px;
  color: #F24B89;
  margin-top: 8px;
}

.buttons {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.buttons button {
  border: 0;
  padding: calc(0.775rem + 1px) calc(1.5rem + 1px);
  border-radius: 6px;
  cursor: pointer;
}

.buttons .cancel {
  border: 1px solid rgb(230, 229, 229);
  background: #fff;
  color: #000;
}

.buttons .submit {
  background: #2997FE;
  color: #fff;
}

.buttons .submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
```

- [ ] **Step 2: Crear el componente**

```tsx
// Frontend/src/components/Modal/Admin/CorregirFecha/index.tsx
import Modal from "react-modal";
import { useEffect, useState } from "react";
import { Icon } from "@iconify/react";
import styles from "./corregirFecha.module.css";

Modal.setAppElement("#root");

interface IProps {
  isOpen: boolean;
  fechaRegistro: string;
  fechaActual: string;
  onGuardar: (nuevaFecha: string) => Promise<void>;
  onClose: () => void;
}

export const CorregirFechaModal = ({
  isOpen,
  fechaRegistro,
  fechaActual,
  onGuardar,
  onClose,
}: IProps) => {
  const [desbloqueado, setDesbloqueado] = useState(false);
  const [fecha, setFecha] = useState(fechaActual);
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setDesbloqueado(false);
      setFecha(fechaActual);
    }
  }, [isOpen, fechaActual]);

  const desbloquear = () => {
    if (
      window.confirm(
        "Vas a desbloquear la fecha para corregirla. ¿Confirmas que deseas continuar?"
      )
    ) {
      setDesbloqueado(true);
    }
  };

  const handleGuardar = async () => {
    setGuardando(true);
    try {
      await onGuardar(fecha);
      onClose();
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onRequestClose={onClose}
      closeTimeoutMS={200}
      className={styles.panel}
      overlayClassName={styles.overlay}
    >
      <div className={styles.encabezado}>
        <h3>Corregir fecha</h3>
        <button type="button" className={styles.closeBtn} onClick={onClose}>
          <Icon icon="mdi:close" width={20} />
        </button>
      </div>

      <div className={styles.campo}>
        <label>Fecha de registro</label>
        <div className={styles.fechaRegistroValor}>{fechaRegistro}</div>
      </div>

      <div className={styles.campo}>
        <label>Fecha de la transacción</label>
        <div className={styles.fechaConCandado}>
          <input
            type="date"
            value={fecha}
            disabled={!desbloqueado}
            onChange={(e) => setFecha(e.target.value)}
          />
          {!desbloqueado && (
            <button type="button" className={styles.unlockBtn} onClick={desbloquear}>
              <Icon icon="mdi:lock-outline" width={18} />
            </button>
          )}
        </div>
        {desbloqueado && (
          <div className={styles.aviso}>
            Modo emergencia: la fecha quedó desbloqueada para corregirla.
          </div>
        )}
      </div>

      <div className={styles.buttons}>
        <button type="button" className={styles.cancel} onClick={onClose}>
          Cancelar
        </button>
        <button
          type="button"
          className={styles.submit}
          disabled={!desbloqueado || guardando}
          onClick={handleGuardar}
        >
          {guardando ? "Guardando..." : "Guardar"}
        </button>
      </div>
    </Modal>
  );
};
```

- [ ] **Step 3: Compilar el frontend**

Run: `npm run build` (desde `Frontend/`)
Expected: build succeeds, sin errores de TypeScript.

- [ ] **Step 4: Commit**

```bash
git -C Frontend add src/components/Modal/Admin/CorregirFecha
git -C Frontend commit -m "feat: add shared CorregirFechaModal component"
```

---

## Task 5: Frontend — Gasto: fecha en creación, grilla con dos columnas, acción de corregir fecha

**Files:**
- Modify: `Frontend/src/components/Modal/Admin/Gasto/index.tsx`
- Modify: `Frontend/src/redux/reducers/Admin/gastos/gasto.reducer.ts`
- Modify: `Frontend/src/presentation/views/Modules/Admin/Views/Gastos/index.tsx`
- Modify: `Frontend/src/presentation/views/Modules/Admin/Views/Gastos/gastos.module.css`

**Interfaces:**
- Consumes: `PUT /api/gastos/modificar-fecha/{id}` (Task 1), `CorregirFechaModal` (Task 4), `GastoDto.fechaRegistro` (Task 1).
- Produces: `actualizarFechaGasto(id, fecha)` thunk — no consumida por otras tareas.

- [ ] **Step 1: Agregar el thunk de corrección**

En `Frontend/src/redux/reducers/Admin/gastos/gasto.reducer.ts`, agregar al final:

```typescript
export const actualizarFechaGasto = (id: number, fecha: string) => {
  return async (dispatch: Dispatch<AnyAction>) => {
    try {
      const response: any = await axiosInstance.put(
        `/gastos/modificar-fecha/${id}`,
        { fecha }
      );
      const { status } = response;
      if (status === 200) {
        toast.success("Fecha corregida correctamente");
        dispatch(getGastos(1, 20) as any);
      }
    } catch (error: any) {
      console.log(error);
      toast.error(error?.response?.data?.message ?? "Error al corregir la fecha");
    }
  };
};
```

- [ ] **Step 2: Agregar los campos de fecha al formulario de creación**

En `Frontend/src/components/Modal/Admin/Gasto/index.tsx`:

Agregar el estado (junto a los demás `useState`, después de `observacion`):

```typescript
  const [fechaGasto, setFechaGasto] = useState<string>("");
```

En el `useEffect` que resetea el formulario (donde se hace `setObservacion("")`), agregar:

```typescript
      setFechaGasto(new Date().toISOString().slice(0, 10));
```

En `handleSubmit`, agregar `fechaGasto` al payload de `crearGasto`:

```typescript
        crearGasto(
          {
            categoria,
            descripcion,
            monto: Number(monto),
            metodoPagoId: metodoPagoId > 0 ? metodoPagoId : null,
            observacion,
            fechaGasto,
          },
          onClose
        ) as any
```

En el JSX, dentro de `<div className={styles.grid}>`, agregar dos campos nuevos antes del campo "Observación":

```tsx
            <div>
              <Input
                isLabel
                label="Fecha de registro"
                type="text"
                name="fechaRegistro"
                value={new Date().toLocaleDateString("es-PE")}
                disabled
              />
            </div>
            <div>
              <Input
                isLabel
                label="Fecha del gasto"
                type="date"
                withDate
                name="fechaGasto"
                value={fechaGasto}
                onChange={(e: any) => setFechaGasto(e.target.value)}
              />
            </div>
```

- [ ] **Step 3: Reemplazar la columna "Fecha" por dos columnas en la grilla, y agregar la acción de corregir fecha**

En `Frontend/src/presentation/views/Modules/Admin/Views/Gastos/index.tsx`:

Agregar el import y el estado de la fila en corrección (junto a `modalOpen`):

```typescript
import { CorregirFechaModal } from "../../../../../../components/Modal/Admin/CorregirFecha";
import { actualizarFechaGasto, anularGasto, getGastos } from "../../../../../../redux/reducers/Admin/gastos/gasto.reducer";
```

```typescript
  const [corrigiendo, setCorrigiendo] = useState<{
    id: number;
    fechaRegistro: string;
    fechaGasto: string;
  } | null>(null);

  const aFechaInput = (fechaDDMMYYYY: string) => {
    const [dia, mes, anio] = fechaDDMMYYYY.split(" ")[0].split("/");
    return `${anio}-${mes}-${dia}`;
  };
```

En el `<thead>`, reemplazar:

```tsx
              <th>Fecha</th>
```

por:

```tsx
              <th>Fecha de registro</th>
              <th>Fecha de gasto</th>
```

En el `<tbody>`, reemplazar:

```tsx
                  <td data-label="Fecha">{g.fechaGasto}</td>
```

por:

```tsx
                  <td data-label="Fecha de registro">{g.fechaRegistro}</td>
                  <td data-label="Fecha de gasto">{g.fechaGasto}</td>
```

En la celda de acciones (donde está el botón "Eliminar"), agregar el botón de corregir fecha antes del botón "Eliminar":

```tsx
                  <td>
                    <button
                      className={styles.editarFechaBtn}
                      onClick={() =>
                        setCorrigiendo({
                          id: g.id,
                          fechaRegistro: g.fechaRegistro,
                          fechaGasto: aFechaInput(g.fechaGasto),
                        })
                      }
                    >
                      Corregir fecha
                    </button>
                    {g.estado !== "ANULADO" && (
                      <button
                        className={styles.anularBtn}
                        onClick={() => confirmarEliminar(g.id)}
                      >
                        Eliminar
                      </button>
                    )}
                  </td>
```

También actualizar `colSpan={8}` de la fila vacía a `colSpan={9}` (una columna más).

Antes del cierre del componente (junto a `<GastoModal .../>`), agregar el modal de corrección:

```tsx
      {corrigiendo && (
        <CorregirFechaModal
          isOpen={!!corrigiendo}
          fechaRegistro={corrigiendo.fechaRegistro}
          fechaActual={corrigiendo.fechaGasto}
          onClose={() => setCorrigiendo(null)}
          onGuardar={(nuevaFecha) =>
            dispatch(actualizarFechaGasto(corrigiendo.id, nuevaFecha) as any)
          }
        />
      )}
```

- [ ] **Step 4: Agregar la clase del nuevo botón**

En `Frontend/src/presentation/views/Modules/Admin/Views/Gastos/gastos.module.css`, agregar junto a `.anularBtn`:

```css
.editarFechaBtn {
  border: 1px solid #2997FE;
  color: #2997FE;
  background: #fff;
  border-radius: 6px;
  padding: 5px 12px;
  font-size: 12px;
  cursor: pointer;
  margin-right: 6px;
}
```

- [ ] **Step 5: Compilar y verificar manualmente**

Run: `npm run build` (desde `Frontend/`)
Expected: build succeeds.

Verificación manual (con `npm run dev` y backend corriendo):
1. Abrir "Nuevo gasto", confirmar que aparecen "Fecha de registro" (deshabilitado, hoy) y "Fecha del gasto" (editable, hoy por defecto); cambiar la fecha, guardar, y confirmar en la grilla que "Fecha de gasto" refleja el valor elegido y "Fecha de registro" muestra la fecha/hora real de creación.
2. En la grilla, click en "Corregir fecha" de un gasto confirmado: el input de fecha debe estar deshabilitado; click en el candado, confirmar el diálogo, cambiar la fecha, Guardar; confirmar que la grilla se refresca con la nueva fecha.
3. Confirmar que un gasto `ANULADO` no permite guardar la corrección (el backend devuelve 400 — debe verse el toast de error).

- [ ] **Step 6: Commit**

```bash
git -C Frontend add src/components/Modal/Admin/Gasto/index.tsx src/redux/reducers/Admin/gastos/gasto.reducer.ts "src/presentation/views/Modules/Admin/Views/Gastos/index.tsx" "src/presentation/views/Modules/Admin/Views/Gastos/gastos.module.css"
git -C Frontend commit -m "feat: fecha fields on create, split grid columns, fecha correction action for Gasto"
```

---

## Task 6: Frontend — Compra: fecha en creación, grilla con dos columnas, acción de corregir fecha

**Files:**
- Modify: `Frontend/src/components/Modal/Admin/Compra/index.tsx`
- Modify: `Frontend/src/redux/reducers/Admin/compras/compra.reducer.ts`
- Modify: `Frontend/src/presentation/views/Modules/Admin/Views/Compras/index.tsx`
- Modify: `Frontend/src/presentation/views/Modules/Admin/Views/Compras/compras.module.css`

**Interfaces:**
- Consumes: `PUT /api/compras/modificar-fecha/{id}` (Task 2), `CorregirFechaModal` (Task 4), `CompraDto.fechaRegistro` (Task 2).
- Produces: `actualizarFechaCompra(id, fecha)` thunk — no consumida por otras tareas.

- [ ] **Step 1: Agregar el thunk de corrección**

En `Frontend/src/redux/reducers/Admin/compras/compra.reducer.ts`, agregar al final:

```typescript
export const actualizarFechaCompra = (id: number, fecha: string) => {
  return async (dispatch: Dispatch<AnyAction>) => {
    try {
      const response: any = await axiosInstance.put(
        `/compras/modificar-fecha/${id}`,
        { fecha }
      );
      const { status } = response;
      if (status === 200) {
        toast.success("Fecha corregida correctamente");
        dispatch(getCompras(1, 20) as any);
      }
    } catch (error: any) {
      console.log(error);
      toast.error(error?.response?.data?.message ?? "Error al corregir la fecha");
    }
  };
};
```

- [ ] **Step 2: Agregar los campos de fecha al formulario de creación**

En `Frontend/src/components/Modal/Admin/Compra/index.tsx`:

Agregar el estado (junto a `observacion`):

```typescript
  const [fechaCompra, setFechaCompra] = useState<string>("");
```

En el `useEffect` que resetea el formulario, agregar:

```typescript
      setFechaCompra(new Date().toISOString().slice(0, 10));
```

En `handleSubmit`, agregar `fechaCompra` al payload de `crearCompra`:

```typescript
        crearCompra(
          {
            proveedorId: proveedorId > 0 ? proveedorId : null,
            metodoPagoId: metodoPagoId > 0 ? metodoPagoId : null,
            observacion,
            fechaCompra,
            detalle: lineasValidas.map((l) => ({
              productoId: l.productoId,
              cantidad: Number(l.cantidad),
              costoUnitario: Number(l.costoUnitario),
            })),
          },
          onClose
        ) as any
```

En el JSX, dentro de `<div className={styles.grid}>`, agregar antes del campo "Observación":

```tsx
            <div>
              <Input
                isLabel
                label="Fecha de registro"
                type="text"
                name="fechaRegistro"
                value={new Date().toLocaleDateString("es-PE")}
                disabled
              />
            </div>
            <div>
              <Input
                isLabel
                label="Fecha de compra"
                type="date"
                withDate
                name="fechaCompra"
                value={fechaCompra}
                onChange={(e: any) => setFechaCompra(e.target.value)}
              />
            </div>
```

- [ ] **Step 3: Reemplazar la columna "Fecha" por dos columnas, y agregar la acción de corregir fecha**

En `Frontend/src/presentation/views/Modules/Admin/Views/Compras/index.tsx`:

```typescript
import { CorregirFechaModal } from "../../../../../../components/Modal/Admin/CorregirFecha";
import { actualizarFechaCompra, anularCompra, getCompras } from "../../../../../../redux/reducers/Admin/compras/compra.reducer";
```

```typescript
  const [corrigiendo, setCorrigiendo] = useState<{
    id: number;
    fechaRegistro: string;
    fechaCompra: string;
  } | null>(null);

  const aFechaInput = (fechaDDMMYYYY: string) => {
    const [dia, mes, anio] = fechaDDMMYYYY.split(" ")[0].split("/");
    return `${anio}-${mes}-${dia}`;
  };
```

En el `<thead>`, reemplazar `<th>Fecha</th>` por:

```tsx
              <th>Fecha de registro</th>
              <th>Fecha de compra</th>
```

En el `<tbody>`, reemplazar `<td data-label="Fecha">{c.fechaCompra}</td>` por:

```tsx
                  <td data-label="Fecha de registro">{c.fechaRegistro}</td>
                  <td data-label="Fecha de compra">{c.fechaCompra}</td>
```

En la celda de acciones, agregar antes del botón "Anular":

```tsx
                  <td>
                    <button
                      className={styles.editarFechaBtn}
                      onClick={() =>
                        setCorrigiendo({
                          id: c.id,
                          fechaRegistro: c.fechaRegistro,
                          fechaCompra: aFechaInput(c.fechaCompra),
                        })
                      }
                    >
                      Corregir fecha
                    </button>
                    {c.estado !== "ANULADO" && (
                      <button
                        className={styles.anularBtn}
                        onClick={() => confirmarAnular(c.id)}
                      >
                        Anular
                      </button>
                    )}
                  </td>
```

Actualizar `colSpan={8}` a `colSpan={9}` en la fila vacía.

Antes del cierre del componente, agregar:

```tsx
      {corrigiendo && (
        <CorregirFechaModal
          isOpen={!!corrigiendo}
          fechaRegistro={corrigiendo.fechaRegistro}
          fechaActual={corrigiendo.fechaCompra}
          onClose={() => setCorrigiendo(null)}
          onGuardar={(nuevaFecha) =>
            dispatch(actualizarFechaCompra(corrigiendo.id, nuevaFecha) as any)
          }
        />
      )}
```

- [ ] **Step 4: Agregar la clase del nuevo botón**

En `Frontend/src/presentation/views/Modules/Admin/Views/Compras/compras.module.css`, agregar (misma regla que Task 5, Step 4):

```css
.editarFechaBtn {
  border: 1px solid #2997FE;
  color: #2997FE;
  background: #fff;
  border-radius: 6px;
  padding: 5px 12px;
  font-size: 12px;
  cursor: pointer;
  margin-right: 6px;
}
```

- [ ] **Step 5: Compilar y verificar manualmente**

Run: `npm run build` (desde `Frontend/`)
Expected: build succeeds.

Verificación manual: mismos 3 pasos que Task 5 Step 5, pero en la pantalla de Compras.

- [ ] **Step 6: Commit**

```bash
git -C Frontend add src/components/Modal/Admin/Compra/index.tsx src/redux/reducers/Admin/compras/compra.reducer.ts "src/presentation/views/Modules/Admin/Views/Compras/index.tsx" "src/presentation/views/Modules/Admin/Views/Compras/compras.module.css"
git -C Frontend commit -m "feat: fecha fields on create, split grid columns, fecha correction action for Compra"
```

---

## Task 7: Frontend — VentasRealizadas: columna Fecha de venta + acción de corregir fecha

**Files:**
- Modify: `Frontend/src/redux/reducers/Admin/ventas/ventasRealizadas.reducer.ts`
- Modify: `Frontend/src/presentation/views/Modules/Admin/Views/VentasRealizadas/index.tsx`

**Interfaces:**
- Consumes: `PUT /api/facturacion/modificar-fecha-venta/{id}` (Task 3), `CorregirFechaModal` (Task 4), `ComprobanteCabeceraDTO.fechaVenta` (Task 3).
- Produces: `actualizarFechaVenta(id, fecha)` thunk — no consumida por otras tareas.

- [ ] **Step 1: Agregar el thunk de corrección**

En `Frontend/src/redux/reducers/Admin/ventas/ventasRealizadas.reducer.ts`, agregar al final. `getAllVentas` necesita `dateStart`/`dateEnd`, y un thunk no tiene acceso al estado del componente que lo despacha, así que el llamador (Step 2) debe pasarlos como parámetros:

```typescript
export const actualizarFechaVenta = (
  idComprobante: number,
  fecha: string,
  dateStart: string,
  dateEnd: string
) => {
  return async (dispatch: Dispatch<AnyAction>) => {
    try {
      const response: any = await axiosInstance.put(
        `/facturacion/modificar-fecha-venta/${idComprobante}`,
        { fecha }
      );
      const { status } = response;
      if (status === 200) {
        dispatch(getAllVentas(dateStart, dateEnd) as any);
      }
    } catch (error: any) {
      console.log(error);
    }
  };
};
```

(Se ignora el bloque intermedio del Step 1 con `dateHistoryGlobal` — no existe, era un paso intermedio erróneo; usar directamente esta segunda versión con los parámetros `dateStart`/`dateEnd`.)

- [ ] **Step 2: Mapear `fechaVenta` en la fila y agregar el estado de corrección**

En `Frontend/src/presentation/views/Modules/Admin/Views/VentasRealizadas/index.tsx`:

Agregar los imports:

```typescript
import { CorregirFechaModal } from "../../../../../../components/Modal/Admin/CorregirFecha";
import {
  activeVentas,
  actualizarFechaVenta,
  getAllVentas,
  openModalVentas,
} from "../../../../../../redux/reducers/Admin/ventas/ventasRealizadas.reducer";
```

Agregar la columna al arreglo `header` (después de `fechaRegistro`):

```typescript
const header: IHeaderTable[] = [
  { type: "id", alias: "N°" },
  { type: "clienteNombre", alias: "Nombres y Apellidos" },
  { type: "tipoDocumento", alias: "TipoDocumento" },
  { type: "serieCorrelativo", alias: "Serie - Correlativo" },
  { type: "nombreVendedor", alias: "Nombre Vendedor" },
  { type: "fechaRegistro", alias: "Fecha Registro" },
  { type: "fechaVenta", alias: "Fecha Venta" },
  { type: "total", alias: "Total" },
  { type: "estadoComprobante", alias: "Estado" },
  { type: "accion", alias: "Accion" },
];
```

En `newDataAnfitrionas`, agregar el campo `fechaVenta` (junto a `fechaRegistro: value?.fecha`):

```typescript
      fechaRegistro: value?.fecha,
      fechaVenta: value?.fechaVenta,
```

Agregar el estado local del modal (junto a `modalOpenMotivo`):

```typescript
  const [corrigiendo, setCorrigiendo] = useState<{
    idComprobante: number;
    fechaRegistro: string;
    fechaVenta: string;
  } | null>(null);

  const aFechaInput = (fechaDDMMYYYY: string) => {
    const [dia, mes, anio] = fechaDDMMYYYY.split(" ")[0].split("/");
    return `${anio}-${mes}-${dia}`;
  };
```

Agregar el handler (junto a `anularVentasMain`):

```typescript
  const corregirFechaMain = (data: any) => {
    setCorrigiendo({
      idComprobante: data?.idComprobante,
      fechaRegistro: data?.fechaRegistro,
      fechaVenta: aFechaInput(data?.fechaVenta ?? data?.fechaRegistro),
    });
  };
```

Agregar la acción al arreglo `buttonsVentas`:

```typescript
  const buttonsVentas: ITableButton[] = [
    {
      title: "Ver Produto",
      icon: "",
      className: "body__btn-companyBtn",
      classNameIcon: "",
      handleOnClick: verProductosMain,
      iconify: "mdi:eye",
    },
    {
      title: "Corregir fecha",
      icon: "",
      className: "body__btn-companyBtn",
      classNameIcon: "",
      handleOnClick: corregirFechaMain,
      iconify: "mdi:pencil-outline",
    },
    {
      title: "Anular",
      icon: "",
      className: "body__btn-companyBtn",
      classNameIcon: "",
      handleOnClick: anularVentasMain,
      iconify: "material-symbols:cancel-rounded",
    },
  ];
```

Antes del cierre del componente (junto a `<VentasModal />`), agregar:

```tsx
      {corrigiendo && (
        <CorregirFechaModal
          isOpen={!!corrigiendo}
          fechaRegistro={corrigiendo.fechaRegistro}
          fechaActual={corrigiendo.fechaVenta}
          onClose={() => setCorrigiendo(null)}
          onGuardar={(nuevaFecha) =>
            dispatch(
              actualizarFechaVenta(
                corrigiendo.idComprobante,
                nuevaFecha,
                dateStart,
                dateEnd
              ) as any
            )
          }
        />
      )}
```

- [ ] **Step 3: Compilar y verificar manualmente**

Run: `npm run build` (desde `Frontend/`)
Expected: build succeeds.

Verificación manual: en la pantalla "Ventas Realizadas", confirmar que aparece la columna "Fecha Venta" junto a "Fecha Registro", que el botón "Corregir fecha" abre el modal con la fecha bloqueada, y que tras desbloquear + guardar la tabla se refresca con la nueva fecha. Confirmar que una venta anulada no aparece en la lista.

- [ ] **Step 4: Commit**

```bash
git -C Frontend add src/redux/reducers/Admin/ventas/ventasRealizadas.reducer.ts "src/presentation/views/Modules/Admin/Views/VentasRealizadas/index.tsx"
git -C Frontend commit -m "feat: fechaVenta column and fecha correction action for VentasRealizadas"
```

---

## Orden de ejecución

Task 1 → Task 2 → Task 3 (backend, independientes entre sí pero cada una es un slice completo) → Task 4 (frontend compartido, sin dependencias de backend) → Task 5, Task 6, Task 7 (cada una depende de su tarea backend correspondiente + Task 4).
