# Fecha de registro bloqueada + fecha de transacción corregible (Gasto, Compra, Venta)

## Contexto

Hoy, al crear un Gasto, Compra o Venta, no hay forma de distinguir la fecha en
que el registro se guardó ("fecha de registro" = `FechaCreacion`, heredada de
`EntityBase`) de la fecha real de la transacción.

- **Gasto** tiene un campo `FechaGasto` (DateTime no nullable, `Gasto.cs:13`) y
  `CreateGastoPayload` ya acepta `FechaGasto` como opcional
  (`CreateGastoPayload.cs:11`). El repositorio lo usa correctamente:
  `FechaGasto = payload.FechaGasto ?? NowLocal()` (`GastoRepository.cs:55`).
  Sin embargo, el formulario del frontend (`components/Modal/Admin/Gasto/index.tsx`)
  no expone este campo — siempre se envía null y cae al fallback `NowLocal()`.
- **Compra** tiene `FechaCompra` (DateTime no nullable, `Compra.cs:13`) pero
  `CreateCompraPayload` no incluye este campo, y el repositorio lo fija
  siempre a `NowLocal()` (`CompraRepository.cs:63`), ignorando cualquier
  fecha real distinta.
- **Venta** (`ComprobanteCabecera`) no tiene ningún campo de fecha de
  transacción separado de `FechaCreacion`. El DTO actual
  (`ComprobanteCabeceraDTO.cs:19`) expone un único campo `Fecha` mapeado
  desde `FechaCreacion` (`MyAutomapper.cs:32`).

Además, ni Gasto ni Compra ni Venta tienen hoy un endpoint de edición — solo
crear y anular. No existe ningún patrón de "campo bloqueado con botón de
desbloqueo de emergencia" en el código existente.

Por último, `ListarCompras` (`CompraRepository.cs:176-208`) y
`ListarComprobantes` (`ComprobanteRepository.cs:184-222`) no filtran por
estado, por lo que hoy muestran registros anulados.

## Objetivo

1. En los formularios de creación de Gasto y Compra, mostrar "Fecha de
   registro" (bloqueada, informativa) y "Fecha del gasto/de compra" (editable,
   por defecto hoy).
2. Permitir corregir la fecha de transacción de un Gasto, Compra o Venta ya
   guardado, mediante una acción de "corregir fecha" que arranca bloqueada y
   requiere un desbloqueo explícito ("modo emergencia") antes de habilitar el
   campo.
3. El desbloqueo no requiere ningún rol especial — cualquier usuario que ya
   puede ver/editar el registro puede desbloquear y corregir la fecha.
4. Para Venta, la corrección de fecha es **solo de presentación**: no debe
   afectar `FechaCreacion` ni ninguna consulta de reporte fiscal/SUNAT
   existente, que siguen usando `FechaCreacion` como fuente de verdad.
5. Ocultar registros anulados en las búsquedas de Compras y de Ventas
   Realizadas (no en Gastos — no fue solicitado).

## Fuera de alcance

- Editar cualquier otro campo del Gasto/Compra/Venta (solo la fecha).
- Cambiar el flujo de creación de Venta (`NuevaVenta` / checkout) — la fecha
  de venta al crear siempre es "ahora", igual que hoy.
- Restricciones de permisos/roles para el desbloqueo.
- Migrar/backfillear datos históricos de `FechaVenta` (se resuelve con
  fallback a `FechaCreacion` cuando es null).
- Tocar Gastos en el filtro de anulados.

## Diseño

### Backend — Gasto

**Entidad** (`Domain/Entities/Gasto.cs`): sin cambios. Ya tiene `FechaGasto`
(DateTime no nullable, línea 13).

**DTO** (`Domain/DTO/GastoDto.cs`): agregar `FechaRegistro` (string) para
exponer la fecha de creación al frontend:

```csharp
public string FechaRegistro { get; set; } = null!; //From FechaCreacion
```

**AutoMapper** (`Domain/Common/Mappings/MyAutomapper.cs`, línea 123): agregar
al mapping `Gasto -> GastoDto`:

```csharp
.ForMember(x => x.FechaRegistro, y => y.MapFrom(z => z.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
```

**Payload** (`Domain/Payloads/ActualizarFechaPayload.cs`): nuevo archivo
compartido entre Gasto, Compra y Venta:

```csharp
namespace Domain.Payloads;

public class ActualizarFechaPayload
{
    public DateTime Fecha { get; set; }
}
```

**Repository** (`Infrastructure/Repositories/GastoRepository.cs`): nuevo
método en `IGastoRepository` y `GastoRepository`:

```csharp
public async Task<(ServiceStatus, string)> ActualizarFechaGasto(int id, DateTime fecha)
{
    var gasto = await _context.Gasto.FirstOrDefaultAsync(g => g.Id == id);
    if (gasto == null)
        return (ServiceStatus.NotFound, "Gasto no encontrado");
    if (gasto.Estado == "ANULADO")
        return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de un gasto anulado");

    gasto.FechaGasto = fecha;
    await _context.SaveChangesAsync();
    return (ServiceStatus.Ok, "Fecha actualizada correctamente");
}
```

**Service** (`Application/Services/GastoService.cs`): passthrough en
`IGastoService` y `GastoService` — firma:
`Task<MessageResult<object>> ActualizarFechaGasto(int id, DateTime fecha)`.

**Controller** (`WEB_API/controllers/GastoController.cs`): nuevo endpoint:

```csharp
[HttpPut("modificar-fecha/{id}")]
public async Task<IActionResult> ActualizarFechaGasto(int id, [FromBody] ActualizarFechaPayload payload)
```

**ListarGastos**: sin cambios (no se pidió ocultar anulados aquí).

### Backend — Compra

**Entidad** (`Domain/Entities/Compra.cs`): sin cambios. Ya tiene `FechaCompra`
(DateTime no nullable, línea 13).

**DTO** (`Domain/DTO/CompraDto.cs`): agregar `FechaRegistro` (string):

```csharp
public string FechaRegistro { get; set; } = null!; //From FechaCreacion
```

**AutoMapper** (`Domain/Common/Mappings/MyAutomapper.cs`, línea 112): agregar
al mapping `Compra -> CompraDto`:

```csharp
.ForMember(x => x.FechaRegistro, y => y.MapFrom(z => z.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
```

**Payload** (`Domain/Payloads/CreateCompraPayload.cs`): agregar campo
opcional:

```csharp
public DateTime? FechaCompra { get; set; }
```

**Repository** (`Infrastructure/Repositories/CompraRepository.cs`):

- `CrearCompra` (línea 63): cambiar `FechaCompra = NowLocal()` por
  `FechaCompra = payload.FechaCompra ?? NowLocal()`.
- Nuevo método `ActualizarFechaCompra` (mismo patrón que Gasto: rechaza si
  `Estado == "ANULADO"`).

**Service** (`Application/Services/CompraService.cs`): passthrough.

**Controller** (`WEB_API/controllers/CompraController.cs`): nuevo endpoint:

```csharp
[HttpPut("modificar-fecha/{id}")]
public async Task<IActionResult> ActualizarFechaCompra(int id, [FromBody] ActualizarFechaPayload payload)
```

**ListarCompras** (`CompraRepository.cs:180`): agregar filtro de anulados:

```csharp
var query = _context.Compra.AsNoTracking().Where(c => c.Estado != "ANULADO").AsQueryable();
```

### Backend — Venta (ComprobanteCabecera)

**Entidad** (`Domain/Entities/ComprobanteCabecera.cs`): agregar campo
nullable:

```csharp
public DateTime? FechaVenta { get; set; }
```

**Migración EF**: agregar columna nullable `FechaVenta` a la tabla de
comprobantes. Sin backfill — los registros históricos quedan null y se
resuelven con fallback en el DTO.

**Repository** (`Infrastructure/Repositories/ComprobanteRepository.cs`):

- `CrearComprobante`: asignar `FechaVenta = NowLocal()` al crear (igual que
  `FechaCreacion`, pero como campo independiente que después se puede corregir
  sin tocar `FechaCreacion`).
- Nuevo método `ActualizarFechaVenta` — rechaza si
  `EstadoComprobante == EstatusComprobante.Anulado`; solo actualiza
  `FechaVenta`. No toca `FechaCreacion` ni ninguna query de reporte/SUNAT.

```csharp
public async Task<(ServiceStatus, string)> ActualizarFechaVenta(int id, DateTime fecha)
{
    var comprobante = await _context.ComprobanteCabecera.FirstOrDefaultAsync(c => c.Id == id);
    if (comprobante == null)
        return (ServiceStatus.NotFound, "Comprobante no encontrado");
    if (comprobante.EstadoComprobante == EstatusComprobante.Anulado)
        return (ServiceStatus.FailedValidation, "No se puede modificar la fecha de un comprobante anulado");

    comprobante.FechaVenta = fecha;
    await _context.SaveChangesAsync();
    return (ServiceStatus.Ok, "Fecha de venta actualizada correctamente");
}
```

**DTO** (`Domain/DTO/ComprobanteCabeceraDto.cs`): agregar campo para
`fechaVenta`:

```csharp
public string FechaVenta { get; set; } // = FechaVenta ?? FechaCreacion
```

El campo `Fecha` existente (línea 19) se mantiene como `FechaCreacion` para
retrocompatibilidad.

**AutoMapper** (`Domain/Common/Mappings/MyAutomapper.cs`, línea 19): agregar
al mapping `ComprobanteCabecera -> ComprobanteCabeceraDTO`:

```csharp
.ForMember(dest => dest.FechaVenta, opt => opt.MapFrom(src =>
    (src.FechaVenta ?? src.FechaCreacion).ToString("dd/MM/yyyy HH:mm:ss")))
```

**Service** (`Application/Services/ComprobanteService.cs`): passthrough.

**Controller** (`WEB_API/controllers/FacturacionController.cs`): nuevo
endpoint:

```csharp
[HttpPut("modificar-fecha-venta/{id}")]
public async Task<IActionResult> ActualizarFechaVenta(int id, [FromBody] ActualizarFechaPayload payload)
```

**ListarComprobantes** (`ComprobanteRepository.cs:205`): agregar filtro de
anulados:

```csharp
var lista = await _context.ComprobanteCabecera.AsNoTracking()
    .Where(x => x.EstadoComprobante != EstatusComprobante.Anulado)
    // ... filtros de fecha existentes ...
```

No se modifican `ListarComprobantesAnulados`,
`ListarComprobantesPendientesEnviarSunat` (jobs internos de SUNAT, deben
seguir viendo anulados/pendientes) ni `VentasRealizadas` (reporte interno).

### Frontend

**Formularios de creación** — `components/Modal/Admin/Gasto/index.tsx` y
`components/Modal/Admin/Compra/index.tsx`:

- Agregar un input deshabilitado "Fecha de registro" (muestra hoy, solo
  informativo).
- Agregar un `<Input type="date">` editable "Fecha del gasto" / "Fecha de
  compra" (por defecto hoy).
- Incluir `FechaGasto` / `FechaCompra` en el payload de creación.

**Componente compartido** — `components/Modal/Admin/CorregirFecha/index.tsx`:

- Props: `fechaRegistro` (string), `fechaActual` (string),
  `onGuardar(nuevaFecha): Promise<void>`, `isOpen`, `onClose`.
- Muestra "Fecha de registro" bloqueada (solo texto).
- Muestra la fecha de transacción arrancando bloqueada (input disabled +
  icono de candado).
- Botón "Desbloquear" pide confirmación y habilita el input.
- Guardar llama a `onGuardar` y cierra; Cancelar descarta el cambio.
- Sin lógica de API propia — cada pantalla conecta su propia llamada.

**Vistas con acción de corregir fecha**:

- `Admin/Views/Gastos/index.tsx`: la grilla hoy tiene una sola columna
  "Fecha" (`g.fechaGasto`, línea 54/74). Reemplazar por dos columnas:
  "Fecha de registro" (`g.fechaRegistro`) y "Fecha de gasto" (`g.fechaGasto`).
  Agregar columna de acciones con icono de lápiz por fila, abre
  `CorregirFechaModal`. Conecta a `actualizarFechaGasto`.
- `Admin/Views/Compras/index.tsx`: la grilla hoy tiene una sola columna
  "Fecha" (`c.fechaCompra`, línea 55/75). Reemplazar por dos columnas:
  "Fecha de registro" (`c.fechaRegistro`) y "Fecha de compra"
  (`c.fechaCompra`). Mismo patrón de acción. Conecta a
  `actualizarFechaCompra`.
- `Admin/Views/VentasRealizadas/index.tsx`: agregar columna de acciones con
  "corregir fecha" (igual que las otras dos) + columna "Fecha de venta" junto
  a la de "Fecha de registro" existente. Conecta a
  `actualizarFechaVenta`.

**Redux** — nuevos thunks:

- `actualizarFechaGasto(id, fecha)`: `PUT /gastos/modificar-fecha/{id}` con
  `{ Fecha: fecha }`, refresca la lista.
- `actualizarFechaCompra(id, fecha)`: `PUT /compras/modificar-fecha/{id}`,
  refresca la lista.
- `actualizarFechaVenta(id, fecha)`: `PUT /facturacion/modificar-fecha-venta/{id}`,
  refresca la lista.

## Archivos a crear/modificar (resumen)

| Capa | Archivo | Acción |
|------|---------|--------|
| Domain | `Payloads/ActualizarFechaPayload.cs` | **Crear** |
| Domain | `Entities/ComprobanteCabecera.cs` | Agregar `FechaVenta` |
| Domain | `DTO/GastoDto.cs` | Agregar `FechaRegistro` |
| Domain | `DTO/CompraDto.cs` | Agregar `FechaRegistro` |
| Domain | `DTO/ComprobanteCabeceraDto.cs` | Agregar `FechaVenta` |
| Domain | `Payloads/CreateCompraPayload.cs` | Agregar `FechaCompra?` |
| Mapping | `Common/Mappings/MyAutomapper.cs` | Actualizar 3 mappings |
| Repo Interface | `IRepository/IGastoRepository.cs` | Agregar `ActualizarFechaGasto` |
| Repo Interface | `IRepository/ICompraRepository.cs` | Agregar `ActualizarFechaCompra` |
| Repo Interface | `IRepository/IComprobanteRepository.cs` | Agregar `ActualizarFechaVenta` |
| Repository | `GastoRepository.cs` | Agregar `ActualizarFechaGasto` |
| Repository | `CompraRepository.cs` | Agregar `ActualizarFechaCompra`, fix `CrearCompra` |
| Repository | `ComprobanteRepository.cs` | Agregar `ActualizarFechaVenta`, fix `CrearComprobante`, fix `ListarComprobantes` |
| Service Interface | `IServices/IGastoService.cs` | Agregar `ActualizarFechaGasto` |
| Service Interface | `IServices/ICompraService.cs` | Agregar `ActualizarFechaCompra` |
| Service Interface | `IServices/IComprobanteService.cs` | Agregar `ActualizarFechaVenta` |
| Service | `GastoService.cs` | Agregar `ActualizarFechaGasto` |
| Service | `CompraService.cs` | Agregar `ActualizarFechaCompra` |
| Service | `ComprobanteService.cs` | Agregar `ActualizarFechaVenta` |
| Controller | `GastoController.cs` | Agregar `PUT modificar-fecha/{id}` |
| Controller | `CompraController.cs` | Agregar `PUT modificar-fecha/{id}` |
| Controller | `FacturacionController.cs` | Agregar `PUT modificar-fecha-venta/{id}` |
| Migration | `Migrations/YYYYMMDD_AddFechaVenta.cs` | **Crear** (columna nullable) |
| Frontend | `components/Modal/Admin/CorregirFecha/index.tsx` | **Crear** |
| Frontend | `components/Modal/Admin/Gasto/index.tsx` | Agregar campos fecha |
| Frontend | `components/Modal/Admin/Compra/index.tsx` | Agregar campo fecha |
| Frontend | `Admin/Views/Gastos/index.tsx` | Agregar columna acciones |
| Frontend | `Admin/Views/Compras/index.tsx` | Agregar columna acciones |
| Frontend | `Admin/Views/VentasRealizadas/index.tsx` | Agregar columna acciones + fechaVenta |
| Frontend | `redux/reducers/Admin/gastos/gasto.reducer.ts` | Agregar `actualizarFechaGasto` |
| Frontend | `redux/reducers/Admin/compras/compra.reducer.ts` | Agregar `actualizarFechaCompra` |
| Frontend | `redux/reducers/Admin/ventas/ventasRealizadas.reducer.ts` | Agregar `actualizarFechaVenta` |

## Notas de implementación

- `ActualizarFechaPayload` es un payload compartido (misma forma exacta para
  los tres endpoints). Se usa `{ "fecha": "2026-08-20" }`.
- Los endpoints PUT usan `{id}` como route param, no como query string (a
  diferencia del patrón `anular?id=` existente). Esto es más RESTful y
  consistente con el patrón `modificar` de `ProductoController`.
- El filtro de anulados en `ListarCompras` se agrega antes de los filtros
  existentes para que no afecte el rendimiento de las consultas paginadas.
- Para `ListarComprobantes`, el filtro se agrega como primer `.Where()` de la
  cadena, antes de los filtros de fecha.
- La migración de `FechaVenta` es unaaddColumn nullable sin default — no
  requiere backfill. Los registros históricos quedan null y el DTO usa
  fallback: `FechaVenta ?? FechaCreacion`.

## Testing

### Backend

- **ActualizarFecha***: verificar que rechaza sobre un registro
  `ANULADO`/anulado (Gasto con `Estado == "ANULADO"`, Compra con
  `Estado == "ANULADO"`, Comprobante con
  `EstadoComprobante == EstatusComprobante.Anulado`).
- **ActualizarFecha***: verificar que acepta sobre un registro confirmado y
  guarda la nueva fecha.
- **ActualizarFechaVenta**: verificar que `FechaCreacion` no se modifica
  después de la actualización.
- **ListarCompras**: verificar que ya no devuelve registros con
  `Estado == "ANULADO"`.
- **ListarComprobantes**: verificar que ya no devuelve registros con
  `EstadoComprobante == Anulado`.
- **CrearCompra**: verificar que `FechaCompra` se usa cuando se envía en el
  payload, y cae a `NowLocal()` cuando es null.
- **CrearComprobante**: verificar que `FechaVenta` se asigna a `NowLocal()`
  al crear.

### Frontend

- Probar flujo de crear Gasto con fecha distinta a hoy (verificar que el
  campo FechaGasto se envía en el payload).
- Probar flujo de crear Compra con fecha distinta a hoy.
- Probar flujo de desbloquear + corregir fecha en un registro ya guardado,
  para Gasto, Compra y Venta.
- Verificar que en VentasRealizadas aparecen las columnas "Fecha de venta" y
  la acción de corregir fecha.
- Verificar que la grilla de Gastos muestra "Fecha de registro" y "Fecha de
  gasto" como columnas separadas, y que la grilla de Compras muestra "Fecha
  de registro" y "Fecha de compra" como columnas separadas.
- Verificar que los registros anulados ya no aparecen en Compras ni en
  VentasRealizadas.
