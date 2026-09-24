# Funcionalidades y reglas del ERP — Compras, Ventas y Cuentas

Documento funcional de los módulos agregados al sistema PuntoVenta, con el modelo de referencia de SAP
Business One. Describe **qué hace cada módulo, cómo se usa y qué reglas aplica el sistema**.
Última actualización: 2026-09-24.

---

## 1. Visión general

El sistema tiene dos formas de trabajar en Compras y en Ventas, elegibles por empresa:

| Modo | Compras | Ventas |
|---|---|---|
| **Simplificado** (por defecto) | La factura del proveedor se registra y el stock sube al guardar. | Cotización → factura o boleta directa; el stock baja al emitir. |
| **Completo** (estilo ERP/SAP) | Orden de compra → Recepción → Factura del proveedor con cruce. | Cotización → Pedido de venta → Entrega → Factura de lo entregado. |

La configuración se cambia en **Configuraciones → Flujo de compras y ventas**. Solo un administrador
puede guardarla. **Cambiar de modo no modifica los documentos ya creados**; solo decide qué pantallas y
reglas se aplican de ahí en adelante.

**Cuentas por cobrar y por pagar** funcionan en ambos modos, porque el crédito existe en los dos.

**La venta de mostrador** (Ir a ventas y Venta rápida) no cambia en ningún modo.

### Correspondencia con SAP Business One

| SAP Business One | En este sistema |
|---|---|
| Cotización de venta | Cotización |
| Pedido de venta (Sales Order) | Pedido de venta |
| Entrega (Delivery) | Entrega |
| Factura de deudores (A/R Invoice) | Factura / Boleta / Nota de venta |
| Pagos recibidos (Incoming Payments) | Cobros (Cuentas por cobrar) |
| Orden de compra (Purchase Order) | Orden de compra |
| Entrada de mercancías (Goods Receipt PO) | Recepción |
| Factura de acreedores (A/P Invoice) | Factura del proveedor |
| Pagos efectuados (Outgoing Payments) | Pagos a proveedor (Cuentas por pagar) |
| Antigüedad de saldos (Aging) | Antigüedad de saldos |

---

## 2. Configuración del flujo

Pantalla: **Configuraciones → Flujo de compras y ventas**.

| Opción | Valores | Efecto |
|---|---|---|
| Flujo de compras | Simplificado / Completo | Activa la pestaña Órdenes y el flujo orden → recepción → factura. |
| Cruce de la factura | Advertir / Bloquear | Qué hace el sistema si la factura del proveedor no coincide con lo recibido o con el precio de la orden. |
| Aprobación por monto | Desactivada / monto | Las órdenes de compra con total mayor al monto necesitan aprobación de un administrador. |
| Flujo de ventas | Simplificado / Completo | Activa Pedidos de venta y la emisión desde entregas. |

Reglas:
- Solo un usuario administrador guarda la configuración (política `RolesPermisosAdmin`).
- Si no hay configuración guardada, todo funciona en modo simplificado.
- En modo simplificado, los endpoints del flujo completo responden *"el flujo completo … no está activo"*.

---

## 3. Compras

### 3.1 Modo simplificado
- **Registrar compra** con: formulario manual, **Subir XML** (uno o varios) o pestañas por tipo de documento
  (Factura, Boleta, Orden Compra, Nota de venta).
- El stock **sube al guardar** la compra; se registra un movimiento de inventario.
- Anular la compra revierte el stock (no permite si dejaría el stock negativo).
- **Carga masiva de XML**: se leen todos los archivos a la vez y se revisan y guardan **de a uno** (cola);
  un XML inválido no detiene a los demás. Las líneas del XML se emparejan con los productos por revisión del usuario.
- **Compra a crédito**: marca "¿Compra al crédito?" y fecha de vencimiento opcional. Exige un proveedor.

### 3.2 Modo completo: Orden → Recepción → Factura
Pantalla: **Compras → pestaña Órdenes** (y el botón "+ Nueva orden de compra").

**Estados de la orden**: Borrador · Pendiente de aprobación · Emitida · Recibida parcial · Recibida · Cerrada · Anulada.

Flujo:
1. **Nueva orden**: proveedor, sucursal y productos con cantidad y costo. Se guarda como **borrador** o se **emite**.
   Si el total supera el monto de aprobación, queda **Pendiente de aprobación** hasta que un administrador la apruebe.
2. **Recibir**: desde una orden emitida o con recepción parcial. Los productos vienen con lo pendiente; se ajustan las
   cantidades. **Aquí sube el stock.**
3. **Facturar**: registra la factura del proveedor (serie, número, fecha, crédito). Compara contra lo recibido y el precio de la orden.
   **La factura no modifica el stock.**
4. **Cierre**: automático cuando todo lo recibido está facturado; o manual con motivo.

Reglas:
- Una orden necesita al menos un producto; cantidades > 0; costo ≥ 0; **un producto no se repite** en la misma orden.
- Solo se edita una orden en **borrador**.
- **Recibir**: solo con orden Emitida o Recibida parcial; **no se puede recibir más de lo pendiente**.
- **Anular recepción**: revierte el stock y las cantidades; no permite si hay facturas sobre esa mercadería; no permite si dejaría el stock negativo.
- **Anular orden**: solo sin recepciones activas y sin facturas.
- **Cruce de la factura**: compara cantidad facturada vs. recibida por facturar y precio facturado vs. precio de la orden.
  - *Advertir*: muestra las diferencias y pide confirmar para guardar.
  - *Bloquear*: rechaza la factura hasta que coincida.
- La factura de una orden **no se edita**: se anula y se registra de nuevo. Anularla reabre lo facturado en la orden (y su estado).
- En modo completo se oculta "Registrar compra" para no mezclar las dos formas de comprar.
- Aprobar una orden requiere permiso de administrador.

Números: `OC-000001` (orden), `REC-000001` (recepción).

---

## 4. Ventas

### 4.1 Modo simplificado
- **Emitir** factura, boleta o nota de venta desde la pantalla de emisión.
- Antes de emitir se elige cómo traer el documento: **Llenar manual**, **Subir XML** (uno o varios) o Buscar en SUNAT / Foto o PDF (*próximamente*).
- Al **subir XML** se precargan cliente y productos (los productos se emparejan por código de barra o nombre; los que no coinciden se avisan).
  Con varios XML se revisan y emiten **de a uno**.
- El stock baja al emitir. Anular la venta lo devuelve.
- **Cotización → Factura / Boleta**: precarga los datos; una cotización vencida, anulada o ya convertida no se convierte.

### 4.2 Modo completo: Pedido → Entrega → Factura
Pantalla: **Venta & Comprobante → Pedidos de venta**.

**Estados del pedido**: Borrador · Confirmado · Entregado parcial · Entregado · Cerrado · Anulado.

Flujo:
1. **Pedido de venta**: cliente y productos con precio. Se crea desde cero o desde una **cotización** (botón "→ Pedido de venta").
   Al **confirmarlo reserva stock**.
2. **Entregar**: total o parcial, con placa y dirección opcionales. **Aquí baja el stock.**
3. **Factura / Boleta**: se emite desde el pedido con lo **entregado y aún sin facturar** (`nueva-factura/…?pedidoVentaId=X`).
   **No baja el stock otra vez.**
4. **Cierre**: automático cuando todo lo entregado está facturado; o manual con motivo.

Reglas:
- **Reserva de stock**: disponible = stock − lo pendiente de entregar de otros pedidos confirmados.
  No se confirma un pedido si el disponible no alcanza.
- **Entrega**: solo desde un pedido Confirmado o Entregado parcial; no excede lo pendiente; debe haber stock real.
- **Anular entrega**: devuelve el stock; no permite si ya hay facturas sobre esa entrega.
- **Facturar un pedido** (validado por el sistema): solo factura, boleta o nota de venta; solo lo entregado sin facturar;
  no se puede facturar más de lo entregado; el producto debe pertenecer al pedido; el modo completo debe estar activo.
- **Anular la factura** de un pedido **no devuelve el stock** (la mercadería sigue entregada): reabre lo facturado y el pedido.
- **Anular pedido**: solo sin entregas activas y sin facturas emitidas.
- Una cotización se convierte **una sola vez** en pedido de venta (mientras el pedido no esté anulado).
- Un producto no se repite en el mismo pedido.

Números: `PV-000001` (pedido), `ENT-000001` (entrega).

---

## 5. Cuentas por cobrar y por pagar

Pantallas: **Cuentas → Cuentas por cobrar** y **Cuentas → Cuentas por pagar**. Cada una con tres pestañas:

1. **Documentos abiertos** (equivale a la ventana de Pagos recibidos / efectuados de SAP): lista de clientes (o proveedores) con su
   saldo y vencido; al elegir uno se ven sus documentos con total, saldo y atraso, una columna **Pagar** por documento (con botón "Todo"),
   medio de pago y referencia.
2. **Cobros / Pagos realizados**: historial con opción de **anular**.
3. **Antigüedad de saldos**: por vencer, 1–30, 31–60, 61–90 y más de 90 días, por cliente o proveedor, con totales.

### Cómo se calcula el saldo (siempre calculado, nunca guardado)
- **Venta a crédito** (factura, boleta o nota de venta, no anulada, con cliente): `total − suma de sus pagos`.
- **Compra a crédito** (no anulada, con proveedor): `total − suma de los pagos a proveedor activos`.
- **Vencimiento**: la fecha de vencimiento del documento; si no tiene, la fecha de emisión.
- El KPI **Crédito por Pagar** de Compras muestra el saldo real.

### Reglas
- Un cobro o pago cubre **varios documentos del mismo cliente o proveedor**, en forma **total o parcial**.
- El monto por documento debe ser mayor a cero y **no puede superar su saldo**. Un documento no se repite en el mismo pago.
- **Venta a crédito**: exige cliente (documento) y **ya no registra un pago inicial** por el total.
  *Antes* lo registraba y el cierre de caja lo contaba como cobrado. Las ventas a crédito emitidas antes de este cambio
  conservan ese pago y quedan como **pagadas** (no aparecen como deuda).
- **Compra a crédito**: exige proveedor.
- **Anular** un cobro o pago **no borra nada**: genera el movimiento inverso y la deuda del documento se reabre.
  Un cobro anulado crea un pago negativo; un pago a proveedor en efectivo anulado crea un retiro negativo.

### Integración con la caja
- **Cobro a cliente (cualquier medio)**: crea un pago por cada documento aplicado, sin caja asignada. **El cierre de caja del
  día lo recoge automáticamente** (efectivo, tarjeta, Yape, transferencia), igual que las ventas.
- **Pago a proveedor en efectivo**: exige que el usuario tenga **caja abierta** y crea un **retiro** con el motivo
  *"Pago proveedor PAG-000001"*. Anular ese pago exige caja abierta y crea el retiro inverso.
- **Otros medios** a proveedor (transferencia, tarjeta, Yape): no afectan la caja.

Números: `COB-000001` (cobro), `PAG-000001` (pago a proveedor).

---

## 6. Sede (sucursal)

- El **desplegable de sede** del encabezado cambia la sede de trabajo; la página se recarga y todas las pantallas piden sus datos con la nueva sede.
- El sistema envía la sede elegida en el encabezado `X-Sucursal`. **Solo se respeta** si el usuario **no tiene sede asignada** o es
  **administrador**; un usuario con sede asignada se queda con la suya aunque intente otra.

---

## 7. Pedidos online (módulo independiente)

Los **Pedidos** online (link público con token, datos del cliente, seguimiento de envío) son un módulo aparte y **no** son el pedido de venta
del flujo completo. Estados: Enviado · Datos completados · En preparación · Despachado · En camino · Entregado · Cancelado.
Al abrir el detalle se pide el pedido completo (con productos) y se muestra un indicador de carga; las operaciones muestran un cargador a pantalla completa.

---

## 8. Reglas transversales

- Todo movimiento de stock genera un **movimiento de inventario** con su referencia (Compra, Recepción, Entrega, Venta, anulaciones).
- Las operaciones de varios pasos son **transaccionales**: si algo falla no queda nada a medias.
- Los documentos **no se borran**; se anulan y se revierte su efecto.
- Los errores devuelven un mensaje claro (400) con el motivo del rechazo.
- Cada documento pertenece a una **empresa (tenant)** y, si corresponde, a una **sede**; los listados respetan ambos.

## 9. Fuera de alcance actual
Pendiente o no incluido:
- **Guía de remisión electrónica (GRE)** ante SUNAT: la *Entrega* es hoy un documento interno.
- **Devolución a proveedores** (salida de mercadería y nota de crédito de compra).
- Pagos a cuenta (anticipos sin factura), aplicar notas de crédito contra el saldo y cuotas de crédito SUNAT.
- **Buscar en SUNAT** y **Foto o PDF** (lectura por IA) al traer un documento.
- Permiso propio para aprobar órdenes (hoy usa el de administrador).

## 10. Referencia técnica

| Área | Endpoints principales |
|---|---|
| Configuración | `GET/PUT /api/configuracion-flujo` |
| Órdenes de compra | `/api/ordenes-compra`: crear, actualizar, listar, {id}, emitir, aprobar, anular, cerrar, {id}/recepciones, recepciones/{id}/anular, {id}/facturar |
| Pedidos de venta | `/api/pedidos-venta`: crear, desde-cotizacion/{id}, actualizar, listar, {id}, confirmar, anular, cerrar, {id}/entregas, entregas/{id}/anular |
| Emisión | `POST /api/facturacion/crear` (acepta `pedidoVentaId`), `POST /api/facturacion/importar-xml` |
| Cuentas por cobrar | `/api/cuentas-por-cobrar`: resumen, cliente/{id}/documentos, antiguedad · `/api/cobranzas`: crear, listar, {id}/anular |
| Cuentas por pagar | `/api/cuentas-por-pagar`: resumen, proveedor/{id}/documentos, antiguedad · `/api/pagos-proveedor`: crear, listar, {id}/anular |

Diseños detallados: `docs/superpowers/specs/` (flujo de compras, flujo de ventas y cuentas por cobrar y pagar).
