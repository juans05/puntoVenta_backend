# Flujo de ventas completo (Pedido de venta → Entrega → Factura) con modo configurable

## Objetivo
Agregar el flujo ERP de ventas y el modo `FlujoVentas` (SIMPLIFICADO | COMPLETO) en `ConfiguracionFlujo`,
junto al de compras. Simplificado no cambia nada de lo actual.

## Decisiones acordadas
- El pedido de venta es un documento nuevo (`PedidoVenta`), separado de los Pedidos online.
- Alcance: Pedido (reserva stock) → Entrega (baja stock) → Factura desde la entrega (no baja stock).
  El cobro sigue como hoy (al emitir). Cuentas por cobrar y GRE electronica quedan fuera.
- La venta de mostrador (Ir a ventas / Venta rapida) no cambia en ningun modo.

## Modelo
- `ConfiguracionFlujo.FlujoVentas` (SIMPLIFICADO por defecto | COMPLETO).
- `PedidoVenta`: Numero, SucursalId, ClienteId?, TipoDocIdentId?, NumeroDocumento, RazonSocial, Direccion,
  FechaEmision, Total, EstadoPedidoVenta, Observacion, CotizacionOrigenId?, MotivoCierre.
  Estados: BORRADOR, CONFIRMADO, ENTREGADO_PARCIAL, ENTREGADO, CERRADO, ANULADO.
- `PedidoVentaDetalle`: PedidoVentaId, ProductoId, CantidadPedida, CantidadEntregada, CantidadFacturada,
  ValorUnitario, TipoIgvId?, UnidadMedidaId?.
- `Entrega`: Numero, PedidoVentaId, Fecha, EstadoEntrega (ACTIVA|ANULADA), Placa?, Direccion?, Observacion?.
- `EntregaDetalle`: EntregaId, PedidoVentaDetalleId, ProductoId, Cantidad.
- `ComprobanteCabecera`: `PedidoVentaId?`, `StockYaDescontado` (bool).

## Reglas
- Confirmar: reserva stock. Reservado = suma de (pedida - entregada) de pedidos CONFIRMADO/ENTREGADO_PARCIAL.
  Disponible = Stock - reservado (de otros pedidos). No confirma si no alcanza.
- Entrega: pedido CONFIRMADO/ENTREGADO_PARCIAL; no excede lo pendiente; stock real suficiente. Baja stock
  (InventoryMovement Venta, ReferenciaTipo="Entrega"). Anular entrega devuelve stock; no si hay facturado sobre ella.
- Factura desde pedido (`pedidoVentaId` en ComprobantePayload, modo COMPLETO): solo lo entregado y no facturado;
  no baja stock (`StockYaDescontado`); suma CantidadFacturada dentro de la transaccion de CrearComprobante.
- Anular comprobante con `StockYaDescontado`: no restaura stock; resta CantidadFacturada y recalcula el pedido.
- Cierre automatico: todo lo entregado facturado y todo entregado. Cierre manual con motivo.
- COMPLETO: "Convertir" cotizacion crea un PedidoVenta (precarga cliente y productos).
- SIMPLIFICADO: endpoints de pedidos-venta responden 400 "flujo completo no activo".

## API
`/api/pedidos-venta`: crear, actualizar (BORRADOR), listar, {id}, {id}/confirmar, {id}/anular, {id}/cerrar,
{id}/entregas, entregas/{id}/anular, {id}/para-factura, desde-cotizacion/{cotizacionId}.
`POST /api/facturacion/crear` acepta `pedidoVentaId`.

## Frontend
Pantalla "Pedidos de venta" (menu Venta & Comprobante): lista, formulario, entrega, facturar (abre
nueva-factura/factura|boleta?pedidoVentaId=X). Selector de FlujoVentas en Configuracion > Flujo.
Cotizaciones: en COMPLETO el boton crea el pedido de venta.

## Pruebas
Reserva de stock, entrega parcial/total, anular entrega, factura sin doble descuento, anular factura de
entrega, modo simplificado intacto.
