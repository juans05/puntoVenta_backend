# ROI de publicidad de Facebook por producto — Design Spec

**Fecha:** 2026-08-25
**Estado:** Aprobado para plan de implementación

## Contexto y objetivo

El usuario exporta desde Meta Ads Manager (Facebook Ads) un Excel con el detalle de
gasto publicitario por anuncio. Hoy no hay forma de relacionar ese gasto con el
costo real de comprar/vender cada producto ni con los ingresos que ese producto
generó, así que no puede saber si un producto es rentable de anunciar o no.

**Objetivo:** una pantalla donde sube ese Excel, asigna cada anuncio a un producto
de su catálogo, y el sistema calcula el ROI real: ingresos por ventas del producto
en el período del anuncio, menos el costo de comprar esas unidades, menos lo
invertido en publicidad.

Este es el primer sub-proyecto de esta línea de trabajo (analítica de
publicidad); no incluye soporte para otras plataformas de ads (Google, TikTok) —
se puede añadir después siguiendo el mismo patrón.

## Columnas reales del Excel de origen (Meta Ads Manager)

```
Inicio del informe, Fin del informe, Nombre del anuncio, Entrega del anuncio,
Resultados, Indicador de resultado, Costo por resultados,
Presupuesto del conjunto de anuncios, Tipo de presupuesto del conjunto de anuncios,
Importe gastado (PEN), Impresiones, Alcance, Contactos de mensajes totales,
Nuevos contactos de mensajes, Compras, Finalización,
Configuración de atribución, Puja, Tipo de puja, Último cambio significativo,
Clasificación de calidad, Clasificación del porcentaje de interacción,
Clasificación del porcentaje de conversiones, Nombre del conjunto de anuncios,
Costo por compra (PEN), Resultados (iniciales), Indicador de resultados (iniciales)
```

No trae "Nombre de la campaña" — la granularidad es por **anuncio**
(`Nombre del anuncio`). Las columnas que el sistema usa activamente:
`Inicio del informe`, `Fin del informe`, `Nombre del anuncio`,
`Nombre del conjunto de anuncios`, `Importe gastado (PEN)`, `Impresiones`,
`Alcance`, `Resultados`, `Costo por resultados`. El resto de columnas de Meta
(puja, clasificación de calidad, etc.) no se usan para el cálculo — se ignoran
al parsear.

## Decisiones de diseño (confirmadas con el usuario)

1. **Cálculo: ROI real**, no solo comparación de costos. Fórmula:
   - `Ingresos` = Σ `ValorUnitarioTotal` de `ComprobanteDetalle` del producto,
     con `ComprobanteCabecera.FechaCreacion` dentro de `[FechaInicio, FechaFin]`
     del anuncio, excluyendo comprobantes con `EstadoComprobante == EstatusComprobante.Anulado`.
   - `CostoProducto` = `Cantidad vendida × Producto.CostoUnitario` (costo
     **actual** del producto, no un snapshot histórico — ver Limitaciones).
   - `GastoAds` = Σ `ImporteGastado` de los registros de `GastoPublicidad` del
     producto en ese rango.
   - `UtilidadNeta = Ingresos − CostoProducto − GastoAds`
   - `RoiPorcentaje = UtilidadNeta / GastoAds` (null/∞ si `GastoAds == 0`,
     el frontend debe manejar ese caso sin dividir por cero)
   - Este cálculo replica el patrón ya usado en `DashboardRepository.cs`
     (`Cantidad * (Producto.CostoUnitario ?? 0)`, filtro
     `EstadoComprobante != EstatusComprobante.Anulado`) — no se inventa un
     patrón nuevo de cálculo de costo/utilidad.

2. **Mapeo anuncio → producto: selección manual al subir.** El nombre del
   anuncio en Facebook no tiene por qué coincidir con el nombre del producto en
   el catálogo, así que no se intenta auto-matching. El usuario asigna cada
   fila a un producto desde un selector en la UI antes de confirmar la carga.

3. **Rango de ventas: solo dentro del rango de fechas del anuncio**
   (`FechaInicio`–`FechaFin` de cada anuncio), no todo el histórico del
   producto. Si el mismo producto tiene varios anuncios con rangos distintos,
   cada anuncio se evalúa contra las ventas de su propio rango (y si dos
   anuncios del mismo producto se solapan en fechas, esas ventas se cuentan en
   ambos — es una simplificación aceptada, no un bug).

4. **Persistencia: sí, se guarda historial.** Cada fila importada se guarda
   como un registro de `GastoPublicidad`. La pantalla permite filtrar por
   producto/rango de fechas sin volver a subir el Excel.

5. **Duplicados: se ignoran automáticamente, no se pide decisión al usuario.**
   Es común volver a exportar/subir el mismo Excel (o uno que se solapa con
   una carga anterior). Cada fila se identifica por
   `HashAnuncio = SHA256(TenantId + NombreAnuncio + FechaInicio + FechaFin)`;
   si ese hash ya existe, la fila se omite en el import (no se duplica el
   gasto ni se rompe el cálculo de ROI) y se informa cuántas filas se
   omitieron. Se prefirió esto sobre un flujo de "actualizar o ignorar" por
   fila — es más simple y cubre el caso real (re-subir el mismo archivo por
   error) sin agregar una decisión más a la UI.

## Modelo de datos

Nueva entidad `GastoPublicidad`, extiende `EntityBase` (ya provee `Id`,
`TenantId`, `FechaCreacion`, `UsuarioCreacion`, `Estado` — multi-tenant y
auditoría gratis, mismo patrón que `Gasto`, `Producto`, etc.):

```csharp
public class GastoPublicidad : EntityBase
{
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public string NombreAnuncio { get; set; } = null!;
    public string? NombreConjuntoAnuncios { get; set; }

    public DateTime FechaInicio { get; set; }   // "Inicio del informe"
    public DateTime FechaFin { get; set; }       // "Fin del informe"

    public decimal ImporteGastado { get; set; }  // "Importe gastado (PEN)"

    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Resultados { get; set; }
    public decimal? CostoPorResultado { get; set; }

    public Guid LoteImportacionId { get; set; }  // agrupa filas de una misma subida

    public string HashAnuncio { get; set; } = null!;  // SHA256(TenantId+NombreAnuncio+FechaInicio+FechaFin), único por fila
}
```

Índice único en `(TenantId, HashAnuncio)` para que la detección de duplicados
sea a nivel de base de datos, no solo una verificación en memoria.

Migración EF Core nueva (`Infrastructure/Migrations`), siguiendo el patrón de
las migraciones existentes del proyecto.

## Backend

**Ubicación:** sigue la arquitectura por capas ya existente
(`Domain.Entities` → `Application.Interfaces`/`Application.Services` →
`Infrastructure.Repositories` → `WEB_API.Controllers`), mismo patrón que
`Gasto`/`GastoRepository`/`GastoController`.

### Endpoints (`WEB_API/Controllers/GastoPublicidadController.cs`)

- **`POST /api/gastopublicidad/importar`**
  Body: lista de filas ya parseadas en el navegador, cada una con
  `ProductoId` ya asignado por el usuario:
  ```json
  {
    "loteImportacionId": "guid",
    "filas": [
      {
        "productoId": 12,
        "nombreAnuncio": "...",
        "nombreConjuntoAnuncios": "...",
        "fechaInicio": "2026-08-01",
        "fechaFin": "2026-08-15",
        "importeGastado": 150.50,
        "impresiones": 12000,
        "alcance": 8000,
        "resultados": 34,
        "costoPorResultado": 4.42
      }
    ]
  }
  ```
  El **parseo del `.xlsx` ocurre en el navegador** (librería `xlsx`, ya
  instalada en `Frontend/node_modules`, hoy usada solo para exportar en
  `ExportExcel.tsx` — se reutiliza para leer, sin agregar dependencia nueva
  ni en frontend ni en backend). El backend nunca recibe el archivo crudo,
  solo JSON ya validado y con producto asignado.

  Validación: `ProductoId` debe existir y pertenecer al tenant actual;
  `FechaFin >= FechaInicio`; `ImporteGastado >= 0`. Calcula `HashAnuncio` por
  fila y omite las que ya existen para el tenant (ver Decisión 5). Inserta el
  resto del lote en una transacción. Responde con conteo de insertadas vs.
  omitidas por duplicado:
  ```json
  { "filasInsertadas": 4, "filasOmitidasPorDuplicado": 1 }
  ```

- **`GET /api/gastopublicidad/roi?desde=&hasta=&productoId=`**
  `desde`/`hasta` filtran por `GastoPublicidad.FechaInicio`/`FechaFin`;
  `productoId` opcional (si se omite, agrupa por todos los productos con
  gasto publicitario en el rango). Devuelve un array:
  ```json
  [
    {
      "productoId": 12,
      "nombreProducto": "...",
      "gastoAds": 320.00,
      "ingresos": 890.00,
      "costoProducto": 410.00,
      "utilidadNeta": 160.00,
      "roiPorcentaje": 0.50
    }
  ]
  ```

- **`GET /api/gastopublicidad`** (listado simple para el historial, paginado
  igual que otros listados del sistema) — filtros por producto/rango de
  fechas/lote de importación.

### Cálculo de ROI

En el repositorio (`GastoPublicidadRepository`), replicar exactamente el
patrón de `DashboardRepository.cs`:

```csharp
var ingresos = await _context.ComprobanteDetalles
    .Where(d => d.ProductoId == productoId
             && d.ComprobanteCabecera.FechaCreacion >= fechaInicio
             && d.ComprobanteCabecera.FechaCreacion <= fechaFin
             && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
    .SumAsync(d => (decimal?)d.ValorUnitarioTotal) ?? 0;

var costoProducto = await _context.ComprobanteDetalles
    .Where(d => /* mismo filtro */)
    .SumAsync(d => (decimal?)(d.Cantidad * (d.Producto.CostoUnitario ?? 0))) ?? 0;
```

## Frontend

**Ubicación:** nueva vista bajo `src/presentation/views/Modules/Admin/Views/`
(mismo patrón que `CatalogosGasto`), con su reducer en
`src/redux/reducers/Admin/gastoPublicidad/` (mismo patrón que
`gastos/gasto.reducer.ts`), usando `axiosInstance` de `utils/axios.ts`.

Flujo de la pantalla ("Publicidad" o "ROI Publicidad" en el menú de Admin):

1. **Botón "Subir Excel"** → input file → `XLSX.read()` parsea el archivo en
   memoria → mapea las columnas conocidas de Meta a los campos del payload.
   Filas con columnas faltantes o `Importe gastado` no numérico se marcan como
   error y se excluyen de la vista previa (no bloquean el resto del archivo).

2. **Tabla de vista previa**: una fila por anuncio encontrado
   (`Nombre del anuncio`, `Nombre del conjunto de anuncios`, fechas, importe
   gastado) con un `<select>` de producto por fila (lista de productos del
   tenant, ya disponible vía el reducer de productos existente). No se puede
   confirmar la carga si alguna fila visible no tiene producto asignado.

3. **Confirmar carga** → `POST /gastopublicidad/importar` → al éxito, dispara
   automáticamente `GET /roi` para el rango de fechas recién importado.

4. **Resumen ROI**: una tarjeta o fila de tabla por producto — Gasto Ads |
   Ingresos | Costo Producto | Utilidad Neta | ROI % — con color verde si
   `utilidadNeta > 0`, rojo si `< 0`. Filtros de fecha/producto arriba para
   revisar el historial sin volver a subir nada.

## Manejo de errores

- Excel sin las columnas mínimas requeridas (`Nombre del anuncio`,
  `Inicio del informe`, `Fin del informe`, `Importe gastado (PEN)`): mensaje
  claro al usuario, no se sube nada.
- Fila con producto sin asignar: bloquea el submit de esa fila (no de todo el
  lote), mensaje inline en la tabla de vista previa.
- `GastoAds == 0` en el cálculo de ROI: el backend devuelve `roiPorcentaje: null`
  en vez de dividir por cero; el frontend muestra "—" en vez de un número.
- Errores del `POST /importar` (producto no existe, fechas inválidas): 400 con
  mensaje por fila, mismo patrón de `ErrorHandler`/`ErrorHandlerMiddleware` ya
  usado en el resto de la API.

## Testing

- Backend: test del cálculo de ROI en `GastoPublicidadRepository` (dado un
  producto con ventas conocidas dentro y fuera del rango, y comprobantes
  anulados que deben excluirse, verificar `ingresos`/`costoProducto` correctos).
  Un test de la validación del import (producto inexistente → rechazado).
  Un test de duplicados (mismo `HashAnuncio` en dos imports → la segunda fila
  se omite, no se duplica el gasto).
- Frontend: test del parser de Excel → payload (dado un Excel de ejemplo con
  las columnas reales de Meta, verificar el mapeo correcto de campos y el
  manejo de filas con columnas faltantes).
- Verificación manual: subir un Excel real de Meta Ads Manager y confirmar que
  el ROI mostrado coincide con un cálculo manual en una hoja de cálculo aparte.

## Fuera de alcance (por ahora)

- Otras plataformas de ads (Google Ads, TikTok Ads) — mismo patrón, distinto
  parser de columnas, se añade como sub-proyecto separado si se necesita.
- Snapshot histórico del costo del producto al momento de la venta (hoy se usa
  el costo *actual* de `Producto.CostoUnitario`; si el costo cambió mucho
  durante el período del anuncio, el número es aproximado).
- Auto-matching de anuncio → producto por nombre (se dejó explícitamente para
  una iteración futura si el mapeo manual resulta tedioso).
- Edición/eliminación de un lote importado ya cargado (se puede agregar
  después si hace falta corregir una carga con datos erróneos).
- Recomendaciones automáticas / detección de anomalías vía IA, y soporte
  multiplataforma (Google Ads, TikTok Ads): evaluados y descartados por ahora
  — no hay historial suficiente todavía para que una recomendación tenga
  sentido, y no hay necesidad real de otra plataforma de ads hoy. La
  arquitectura no lo bloquea si se necesita después.