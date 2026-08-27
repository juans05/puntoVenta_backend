# Fecha de registro bloqueada + fecha de transacción corregible (Gasto, Compra, Venta)

## Contexto

Hoy, al crear un Gasto, Compra o Venta, no hay forma de distinguir la fecha en
que el registro se guardó ("fecha de registro" = `FechaCreacion`, heredada de
`EntityBase`) de la fecha real de la transacción. Gasto ya tiene un campo
`FechaGasto` separado (aceptado opcionalmente en el payload de creación, pero
nunca expuesto en el formulario del frontend). Compra tiene `FechaCompra` pero
el repositorio lo fija siempre a `NowLocal()`, ignorando cualquier fecha real
distinta. Venta (`ComprobanteCabecera`, el comprobante fiscal enviado a SUNAT)
no tiene ningún campo de fecha de transacción separado de `FechaCreacion`.

Además, ni Gasto ni Compra ni Venta tienen hoy un endpoint de edición — solo
crear y anular. No existe ningún patrón de "campo bloqueado con botón de
desbloqueo de emergencia" en el código existente.

Por último, `ListarCompras` y `ListarComprobantes` (que alimenta la pantalla
de Ventas Realizadas) no filtran por estado, por lo que hoy muestran registros
anulados.

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

- `Domain/DTO/GastoDto.cs`: agregar `FechaRegistro` (mapeado desde
  `FechaCreacion`).
- Nuevo `Domain/Payloads/ActualizarFechaPayload.cs`: `{ DateTime Fecha }`
  (compartido entre Gasto, Compra y Venta — misma forma exacta).
- `IGastoRepository` / `GastoRepository`: nuevo método
  `ActualizarFechaGasto(int id, DateTime fecha)` — carga el gasto, rechaza si
  `Estado == "ANULADO"`, asigna `FechaGasto = fecha`, guarda.
- `IGastoService` / `GastoService`: passthrough.
- `GastoController`: `PUT /api/gastos/{id}/fecha`.
- `GastoRepository.ListarGastos`: sin cambios (no se pidió ocultar anulados
  aquí).

### Backend — Compra

- `Domain/DTO/CompraDto.cs`: agregar `FechaRegistro` (desde `FechaCreacion`).
- `Domain/Payloads/CreateCompraPayload.cs`: agregar `DateTime? FechaCompra`.
- `CompraRepository.CrearCompra`: cambiar `FechaCompra = NowLocal()` por
  `FechaCompra = payload.FechaCompra ?? NowLocal()`.
- Nuevo método `ActualizarFechaCompra(int id, DateTime fecha)` (mismo patrón
  que Gasto: rechaza si `Estado == "ANULADO"`).
- `CompraController`: `PUT /api/compras/{id}/fecha`.
- `CompraRepository.ListarCompras`: agregar
  `.Where(c => c.Estado != "ANULADO")`.

### Backend — Venta (ComprobanteCabecera)

- `Domain/Entities/ComprobanteCabecera.cs`: agregar `DateTime? FechaVenta`.
- Migración EF: agregar columna nullable `FechaVenta` a la tabla de
  comprobantes (sin backfill — los registros históricos quedan null y se
  resuelven con fallback en el DTO).
- `ComprobanteRepository.CrearComprobante`: asignar `FechaVenta = NowLocal()`
  al crear (igual que `FechaCreacion` al inicio, pero como campo
  independiente que después se puede corregir sin tocar `FechaCreacion`).
- Nuevo método `ActualizarFechaVenta(int id, DateTime fecha)` — rechaza si
  `EstadoComprobante == EstatusComprobante.Anulado`; solo actualiza
  `FechaVenta`. No toca `FechaCreacion` ni ninguna query de reporte/SUNAT.
- `FacturacionController`: `PUT /api/facturacion/{id}/fecha-venta`.
- `ComprobanteCabeceraDto` (o el DTO que use `ListarComprobantes`): agregar
  `fechaVenta` (= `FechaVenta ?? FechaCreacion`) junto al `fechaRegistro`
  (`FechaCreacion`) existente.
- `ComprobanteRepository.ListarComprobantes`: agregar
  `.Where(x => x.EstadoComprobante != EstatusComprobante.Anulado)`.
- No se modifican `ListarComprobantesAnulados`,
  `ListarComprobantesPendientesEnviarSunat` (jobs internos de SUNAT, deben
  seguir viendo anulados/pendientes) ni `ObtenerCompra` (no es una búsqueda).

### Frontend

- `components/Modal/Admin/Gasto/index.tsx` y `.../Compra/index.tsx`
  (formularios de creación): agregar un input deshabilitado "Fecha de
  registro" (muestra hoy) + un `<Input type="date">` editable "Fecha del
  gasto" / "Fecha de compra" (por defecto hoy), incluido en el payload de
  creación.
- Nuevo componente compartido `CorregirFechaModal`
  (`components/Modal/Admin/CorregirFecha/index.tsx`): recibe `fechaRegistro`,
  `fechaActual`, y `onGuardar(nuevaFecha): Promise<void>`. Muestra
  "Fecha de registro" bloqueada (solo texto) y la fecha de transacción
  arrancando bloqueada (input disabled + icono de candado); un botón
  "Desbloquear" pide confirmación y habilita el input; Guardar llama a
  `onGuardar` y cierra; Cancelar descarta el cambio. Sin lógica de API propia
  — cada pantalla que lo use conecta su propia llamada.
- `Admin/Views/Gastos/index.tsx` y `Admin/Views/Compras/index.tsx`: agregar
  acción de "corregir fecha" (icono de lápiz) por fila, abre
  `CorregirFechaModal`.
- `Admin/Views/VentasRealizadas/index.tsx`: hoy es una tabla de solo lectura
  sin columna de acciones — agregar columna de acciones con "corregir fecha"
  igual que las otras dos, más una columna "Fecha de venta" junto a la de
  "Fecha de registro" existente.
- Nuevos thunks redux: `actualizarFechaGasto`, `actualizarFechaCompra`,
  `actualizarFechaVenta`, cada uno llamando a su endpoint PUT respectivo.

## Testing

- Backend: verificar que `ActualizarFecha*` rechaza sobre un registro
  `ANULADO`/anulado. Verificar que `ListarCompras`/`ListarComprobantes` ya no
  devuelven anulados. Verificar que `ActualizarFechaVenta` no modifica
  `FechaCreacion`.
- Frontend: probar manualmente el flujo de crear con fecha distinta a hoy, y
  el flujo de desbloquear + corregir fecha en un registro ya guardado, para
  Gasto, Compra y Venta.
