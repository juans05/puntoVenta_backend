# Flujo de compras completo (OC → Recepción → Factura) con modo configurable

## Objetivo
Agregar el flujo ERP de compras (orden de compra, recepción, factura del proveedor con cruce) y un
interruptor en Configuración: `SIMPLIFICADO` (comportamiento actual, intacto) o `COMPLETO`.

## Decisiones acordadas
- Alcance: OC → Recepción (total o parcial) → Factura con cruce. Sin pagos/cuentas por pagar (pieza aparte).
- Cruce factura vs. orden/recepción configurable: `ADVERTIR` (defecto) o `BLOQUEAR`.
- Aprobación de OC configurable por monto (`MontoAprobacionOc`; vacío = nunca).
- Enfoque A: dos caminos separados. Simplificado no cambia; completo usa tablas nuevas.
- Flujo de ventas completo queda fuera (pieza posterior).

## Modelo de datos
- `ConfiguracionFlujo` (una fila por tenant): `FlujoCompras` (SIMPLIFICADO|COMPLETO), `CruceFactura` (ADVERTIR|BLOQUEAR), `MontoAprobacionOc` (decimal?).
- `OrdenCompra`: Numero, ProveedorId, SucursalId, MonedaId, FechaEmision, Total, Estado, Observacion, AprobadoPor, FechaAprobacion, MotivoCierre.
  Estados: BORRADOR, PENDIENTE_APROBACION, EMITIDA, RECIBIDA_PARCIAL, RECIBIDA, CERRADA, ANULADA.
- `OrdenCompraDetalle`: OrdenCompraId, ProductoId, CantidadPedida, CantidadRecibida, CostoUnitario.
- `Recepcion`: Numero, OrdenCompraId, Fecha, Estado (ACTIVA|ANULADA), Observacion.
- `RecepcionDetalle`: RecepcionId, OrdenCompraDetalleId, ProductoId, Cantidad.
- `Compra` (factura): `OrdenCompraId` (nullable), `StockYaIngresado` (bool, defecto false).

## Reglas
- Recepción: solo con orden EMITIDA o RECIBIDA_PARCIAL; no excede lo pendiente. Sube stock y registra
  `InventoryMovement` (Compra, ReferenciaTipo="Recepcion"). Orden pasa a RECIBIDA_PARCIAL/RECIBIDA.
- Anular recepción: revierte stock y cantidades recibidas; recalcula estado de la orden.
- Anular orden: solo sin recepciones activas.
- Factura con `OrdenCompraId` (modo completo): no toca stock (`StockYaIngresado = true`); cruza cantidad
  facturada vs recibida y precio vs orden. `BLOQUEAR` rechaza (400); `ADVERTIR` acepta si `confirmarDiferencias`.
- Cierre automático: orden RECIBIDA cuyo total recibido está facturado pasa a CERRADA. Cierre manual con motivo.
- Modo SIMPLIFICADO: endpoints de órdenes responden 400 "el flujo completo no está activo"; `CrearCompra` igual que hoy.
- Aprobación: total > MontoAprobacionOc → PENDIENTE_APROBACION; solo usuario con permiso aprueba.

## API
`/api/ordenes-compra`: `POST crear`, `PUT actualizar/{id}` (BORRADOR), `GET listar`, `GET {id}`,
`PUT {id}/aprobar`, `PUT {id}/anular`, `PUT {id}/cerrar`, `POST {id}/recepciones`,
`PUT recepciones/{id}/anular`, `GET {id}/para-factura`. `GET/PUT /api/configuracion-flujo`.
`POST /api/compras/crear` acepta `ordenCompraId` y `confirmarDiferencias`.

## Frontend
Pestaña "Órdenes" de Compras (lista, formulario, diálogo de recepción, factura desde orden con bloque de
cruce) y sección "Flujo de compras" en Configuración. En simplificado la pestaña indica activar el flujo.

## Pruebas
Recepción parcial→total (stock y estado), aprobación por monto, cruce ADVERTIR/BLOQUEAR, anular recepción
revierte stock, modo simplificado sin cambios.
