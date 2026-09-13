# Nueva emisión de factura (rediseño de NuevaFactura) — design

Fecha: 2026-09-13
Estado: aprobado por el usuario en chat, pendiente de plan de implementación.

## Objetivo

Reemplazar la pantalla actual de `NuevaFactura` por un formulario más
completo, calcado del layout de referencia que pasó el usuario: captura de
cliente con dirección/ubigeo/celular, búsqueda por documento, lista de
productos con IGV/unidad editables por línea, multipagos, crédito,
descuento, cálculo de vuelto, y tres modos de envío del comprobante
(guardar solo, enviar a SUNAT ahora, o firmar/imprimir sin enviar).

## Alcance

**Incluye:** todos los campos del mockup con lógica real (no cosméticos);
endpoint unificado de búsqueda de cliente por documento con punto de
extensión para una API externa de RENIEC/SUNAT; nuevo estado
`EnviadoSunat = NoEnviar` para el modo "solo firmar e imprimir"; envío
síncrono a SUNAT como alternativa al job asíncrono existente; descuento
porcentual, flag de crédito, y cálculo/validación de vuelto persistidos en
`ComprobanteCabecera`; IGV y unidad de medida editables por línea de
producto, ya no hardcodeados en `ArmarInvoice`.

**No incluye (fuera de alcance, YAGNI):**
- Integración real con un proveedor de consulta RENIEC/SUNAT (apis.net.pe,
  Decolecta, Factiliza, etc.) — hoy no hay token/proveedor elegido. Se deja
  un punto de extensión (`IConsultaDocumentoProxy`) para que conectarlo
  después sea un cambio aislado, sin tocar el resto del flujo.
- Módulo de cuentas por cobrar para ventas al crédito (vencimientos, abonos
  parciales, recordatorios). Solo se persiste el flag `EsCredito` y se
  omite la validación de vuelto; el seguimiento de cobranza queda para otra
  iteración si se necesita.
- Catálogo completo de códigos SUNAT de unidad de medida/afectación IGV
  (son decenas). Se implementa una lista corta con los casos reales del
  negocio (Gravado/Exonerado/Inafecto; Unidad/Servicio/Kilogramo),
  ampliable después sin cambiar el modelo de datos (son strings, no enums
  de C#).
- Reenvío de comprobante por email: el toggle "¿Enviar el comprobante al
  email del cliente?" se persiste (`EnviarComprobanteEmail`) pero el envío
  de email en sí no está construido — no existe hoy ningún servicio de
  email saliente en el proyecto. Se deja el flag para no rehacer el
  formulario cuando se agregue esa pieza.

## Decisiones confirmadas

| Pregunta | Decisión |
|---|---|
| ¿Qué hace la lupa junto a RUC/DNI? | Si hay token de RENIEC/SUNAT configurado, consulta la API externa; si no (caso de hoy), busca en la BD de clientes por documento exacto. |
| ¿Cómo funciona "modo de envío"? | Mapea a la infraestructura ya existente: Solo Guardar = comportamiento actual (job asíncrono la recoge); Enviar a SUNAT ahora = envío síncrono en el momento; Solo Firmar e Imprimir = nunca se envía (estado nuevo `NoEnviar`, excluido del job). |
| ¿Descuento/crédito/vuelto/multipagos son funcionales o solo visuales? | Funcionales: afectan el cálculo real y quedan persistidos para reportes. |

## Diseño

### 1. Cambios en `ComprobanteCabecera` (nueva migración)

```csharp
public string? Direccion { get; set; }
public string? UbigeoId { get; set; }
public string? Celular { get; set; }
public bool EnviarComprobanteEmail { get; set; }
public bool EsCredito { get; set; }
public decimal? DescuentoPorcentaje { get; set; }
public decimal? DescuentoTotal { get; set; }
public decimal? TotalRecibido { get; set; }
public decimal? Vuelto { get; set; }
public string? Observacion { get; set; }
public char ModoEnvio { get; set; } = EstatusModoEnvio.SoloGuardar;

public Ubigeo? Ubigeo { get; set; }
```

`Direccion`/`UbigeoId`/`Celular` son un snapshot al momento de la venta
(mismo patrón que `Pedido`), no se leen de `Cliente` — un cliente puede
mudarse o el dato puede venir corregido a mano en esta venta puntual.

```csharp
public static class EstatusModoEnvio
{
    public const char SoloFirmarImprimir = 'F';
    public const char EnviarAhora = 'S';
    public const char SoloGuardar = 'G';
}
```

Nuevo valor en `EstatusEnvioSunat` (`Domain/Enumerations/Enums.cs`):

```csharp
public const char NoEnviar = 'N';
```

`ComprobanteRepository.ListarComprobantesPendientesEnviarSunat` ya filtra
por `EnviadoSunat == Pendiente`, así que las filas en `NoEnviar` quedan
excluidas del job sin tocar esa query.

### 2. Cambios en `ComprobanteDetalle` / línea de producto

```csharp
public string TipoIgv { get; set; } = "10";   // código SUNAT: 10 Gravado, 20 Exonerado, 30 Inafecto
public string UnidadMedida { get; set; } = "NIU"; // NIU Unidad, ZZ Servicio, KGM Kilogramo
```

`ArmarInvoice`/`ArmarDetails` (en `ComprobanteRepository` e `InvoiceJob`,
hoy duplicados) dejan de hardcodear `unidad = "NIU"` y `tipAfeIgv = "10"` y
usan estos campos por línea.

### 3. Payloads

```csharp
public class ComprobantePayload
{
    // ... campos existentes ...
    public string? Direccion { get; set; }
    public string? UbigeoId { get; set; }
    public string? Celular { get; set; }
    public bool EnviarComprobanteEmail { get; set; }
    public bool EsCredito { get; set; }
    public decimal? DescuentoPorcentaje { get; set; }
    public decimal? TotalRecibido { get; set; }
    public string? Observacion { get; set; }
    public char ModoEnvio { get; set; } = EstatusModoEnvio.SoloGuardar;
}

public class ComprobanteDetallePayload
{
    // ... campos existentes ...
    public string TipoIgv { get; set; } = "10";
    public string UnidadMedida { get; set; } = "NIU";
}
```

`DetallePago` no cambia de forma — el backend ya acepta una lista, el
cambio de "multipagos" es solo que el frontend permita agregar más de una
línea en vez de mandar siempre una sola.

### 4. Lógica en `CrearComprobante`

Orden de cálculo (extiende el que ya existe):
1. `sumatoria = Σ (ValorUnitario × Cantidad)` de los detalles (igual que hoy).
2. `descuentoTotal = Math.Round(sumatoria × (DescuentoPorcentaje ?? 0) / 100, 2)`.
3. `total = sumatoria - descuentoTotal` — este es el `payload.Total` que ya
   se validaba contra la suma de detalles; la validación existente se
   ajusta para comparar contra `total` (post-descuento), no contra la
   suma cruda.
4. Split subtotal/IGV: igual que hoy, a partir de `total`.
5. Si `!EsCredito`: exige `TotalRecibido.HasValue && TotalRecibido >= total`
   (si no, `FailedValidation`, "El monto recibido es menor al total").
   `Vuelto = TotalRecibido - total`.
6. Si `EsCredito`: `TotalRecibido`/`Vuelto` quedan `null`, sin validar.
7. Según `ModoEnvio`:
   - `SoloGuardar` → `EnviadoSunat = Pendiente` (sin cambios, el job la
     recoge después).
   - `SoloFirmarImprimir` → `EnviadoSunat = NoEnviar`.
   - `EnviarAhora` → `EnviadoSunat = Pendiente` al guardar, y **después**
     del commit de la transacción se arma el `InvoiceRequest` (reusando
     `ArmarInvoice`, ya privado en `ComprobanteRepository`) y se llama
     `IFacturacionProxy.EnviarComprobanteSunar` en el momento, actualizando
     `EnviadoSunat`/`MensajeSunat` con la respuesta — mismo resultado que
     el job, pero inmediato. Requiere inyectar `IFacturacionProxy` en
     `ComprobanteRepository` (hoy solo lo tiene `ComprobanteService`/
     `InvoiceJob`).

### 5. Endpoint de búsqueda por documento

`GET /api/clientes/consultar-documento?tipoDoc={dni|ruc}&numero=`

```csharp
public interface IConsultaDocumentoProxy
{
    Task<ConsultaDocumentoResult?> Consultar(string tipoDoc, string numero);
}
```

Implementación por defecto (`ClienteRepository` o servicio nuevo,
a definir en el plan): si `config["ConsultaDocumento:Token"]` no está
seteado, no se llama a ningún proxy — se busca directo en
`_context.Cliente` por `NumeroDocumento` (igual que hoy hace
`/clientes/listar?value=`, pero por documento exacto en vez de fuzzy) y se
devuelve `null` si no hay match, para que el frontend lo complete a mano.
El proxy real queda sin implementar (no hay contrato de API elegido).

### 6. Frontend — reescritura de `NuevaFactura`

Estructura calcada del mockup:

- **Cliente**: Tipo Doc. Identidad (select del catálogo `TipoDocumento`
  existente), N° de documento + botón lupa → llama al endpoint del punto
  5 y autocompleta Razón Social/Dirección si hay match; Razón Social,
  Dirección, Ubigeo (select, reusa `/extensiones/ubigeos`), Celular; toggle
  "enviar comprobante al email".
- **Lista de productos**: tabla con Descripción, Tipo IGV (select corto),
  Und/Medida (select corto), Precio, Cantidad, Sub.Total, IGV, Importe;
  botones Editar/Agregar (F1)/Eliminar sobre la fila seleccionada (hoy solo
  hay "Agregar" vía modal — se agrega selección de fila + editar/eliminar
  puntual).
- **Multipagos**: toggle que expande la lista de `DetallePago` a N líneas
  (método + monto), validando que la suma de montos calce con el total.
- **Crédito / Forma de pago / Descuento / Recibido / Vuelto**: como en el
  mockup, con el cálculo del punto 4 reflejado en vivo.
- **Resumen**: Gravada (subtotal), Descuento Total, IGV (18%), Total.
- **Observación**: textarea libre.
- **Modo de envío**: radio de 3 opciones, mapeado al punto 4.
- Botón único `GUARDAR DOCUMENTO ELECTRÓNICO (F12)` — atajo de teclado
  incluido, ya que el mockup lo marca explícitamente.

### 7. Errores y casos borde

- Vuelto negativo en venta no-crédito → `FailedValidation` antes de tocar
  la BD (misma capa que ya valida el total contra el detalle).
- `ModoEnvio = EnviarAhora` pero `IFacturacionProxy` falla o lanza → la
  venta ya quedó guardada (no se revierte la transacción por un fallo de
  red externo); `EnviadoSunat` pasa a `Error` con el mensaje, igual que hoy
  hace el job — el usuario ve el comprobante como "guardado, pendiente de
  reintento" en vez de perder la venta.
- Búsqueda por documento sin match y sin token configurado → 200 con
  `data: null`, el frontend simplemente no autocompleta nada (no es un
  error, es un "no encontrado, complétalo tú").
- Multipagos cuya suma no calza con el total → validación en frontend
  antes de habilitar "Guardar", más validación defensiva en backend
  (`Σ DetallePago.Monto == total`, con la misma tolerancia de redondeo a
  centavos que ya usa la comparación de `Total` contra el detalle).

### 8. Testing

- Backend: descuento aplicado correctamente al total/subtotal/IGV; crédito
  omite validación de vuelto; venta no-crédito con recibido menor al total
  falla; `ModoEnvio = SoloFirmarImprimir` deja `EnviadoSunat = NoEnviar` y
  queda fuera de `ListarComprobantesPendientesEnviarSunat`; `ModoEnvio =
  EnviarAhora` invoca `IFacturacionProxy` de forma síncrona (mock) y
  refleja la respuesta en `EnviadoSunat`; `TipoIgv`/`UnidadMedida` por
  línea llegan tal cual al `InvoiceRequest.details`.
- Frontend: smoke manual en navegador del formulario completo (todos los
  campos del mockup, ambos flujos de crédito/contado, los tres modos de
  envío) — no hay test suite de frontend en el proyecto hoy.
