# Cuentas por cobrar y por pagar (estilo SAP Business One)

## Objetivo
Registrar el credito: saldo por documento, cobros/pagos parciales o multiples, estado de cuenta y
antiguedad de saldos, integrado con la caja. Disponible en modo simplificado y completo.

## Decisiones acordadas
- Efectivo afecta la caja del dia (cobros: via `Pago`; pagos a proveedor: via `Retiros`).
- Una venta a credito ya NO crea el `Pago` inicial por el total (antes inflaba el cierre de caja).
- Historico: ventas a credito ya emitidas conservan su `Pago` por el total y quedan como pagadas.
- Fuera de alcance: pagos a cuenta (anticipos), aplicar notas de credito al saldo, cuotas SUNAT.

## Modelo
- `Cobranza`: Numero, ClienteId, Fecha, MetodoPagoId, Monto, Referencia?, Observacion?, EstadoCobranza (ACTIVA|ANULADA).
- `CobranzaDetalle`: CobranzaId, ComprobanteCabeceraId, MontoAplicado.
- `PagoProveedor`: Numero, ProveedorId, Fecha, MetodoPagoId, Monto, Referencia?, Observacion?, EstadoPago (ACTIVO|ANULADO).
- `PagoProveedorDetalle`: PagoProveedorId, CompraId, MontoAplicado.
- `Compra.FechaVencimiento?`.

## Saldos (calculados, nunca guardados)
- Venta: documentos Factura/Boleta/Nota de venta con `EsCredito`, no anulados, con ClienteId:
  saldo = ValorTotal - suma(Pago.Monto) (los cobros crean Pago; las anulaciones crean Pago negativo).
- Compra: `EsCredito`, no anulada, con ProveedorId: saldo = Total - suma(PagoProveedorDetalle de pagos ACTIVOS).
- Vencimiento para antiguedad: FechaVencimiento, o fecha de emision/venta si no tiene.

## Reglas
- Un cobro/pago aplica a varios documentos del mismo socio; monto por documento > 0 y <= saldo; suma = Monto.
- Cobro: por cada documento aplicado crea un `Pago` (metodo del cobro, CajaId null: el cierre de caja lo recoge).
- Pago a proveedor en efectivo: exige caja abierta del usuario; crea un `Retiros` (Motivo "Pago proveedor <N>").
- Anular: no borra; cobro -> Pago negativo por cada detalle; pago a proveedor en efectivo -> Retiro negativo
  (exige caja abierta). El documento vuelve a tener saldo.
- Venta a credito exige cliente (documento). Compra a credito exige proveedor.

## API
`/api/cuentas-por-cobrar`: `resumen`, `cliente/{id}/documentos`, `antiguedad`.
`/api/cuentas-por-pagar`: `resumen`, `proveedor/{id}/documentos`, `antiguedad`.
`/api/cobranzas`: `POST crear`, `GET listar`, `PUT {id}/anular`.
`/api/pagos-proveedor`: `POST crear`, `GET listar`, `PUT {id}/anular`.

## Frontend
Grupo de menu "Cuentas": "Cuentas por cobrar" y "Cuentas por pagar", cada una con tabs: Documentos abiertos
(socios con saldo -> documentos con columna "Pagar", medio de pago, referencia), Pagos realizados (con anular),
Antiguedad de saldos. KPI "Credito por Pagar" de Compras usa el saldo real.

## Pruebas
Saldo de venta y compra a credito; cobro parcial y total; cobro aplicado a varias facturas; rechazo por
exceder saldo; anular cobro reabre la deuda; efectivo de pago a proveedor exige caja y crea retiro;
venta a credito no crea Pago inicial; antiguedad por tramos.
