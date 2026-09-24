# Guía de remisión (documento interno, sin envío a SUNAT)

## Objetivo
Generar una guía de remisión remitente a partir de una Entrega del flujo de ventas completo, como
constancia interna de traslado. Explícitamente NO se envía a SUNAT: no hay proxy, credenciales,
ticket ni estado de SUNAT.

## Modelo
- `GuiaRemision`: Numero, EntregaId (1 activa por entrega), SucursalId, FechaEmision, FechaTraslado,
  ClienteNombre, ClienteDocumento (snapshot del pedido, por si el cliente cambia despues),
  Motivo (01 Venta por defecto), ModTraslado (01 publico | 02 privado), PesoTotal?, UndPesoTotal?,
  UbigeoPartida?, DireccionPartida?, UbigeoLlegada?, DireccionLlegada? (Llegada default = Entrega.Direccion),
  TransportistaRuc/RazonSocial/Mtc (publico) o ChoferNombre/ChoferDocumento/Placa (privado, Placa default
  = Entrega.Placa), EstadoGuia (EMITIDA|ANULADA).
- `GuiaRemisionDetalle`: ProductoId, Cantidad, Unidad (copiados de EntregaDetalle al generar).

## Reglas
- Se genera desde una Entrega ACTIVA sin guia activa ya generada (1 a 1).
- Al generar se copian cliente, direccion y placa del pedido/entrega; el resto (motivo, modTraslado,
  transportista/chofer, peso) se completa a mano.
- Anular es local (no hay SUNAT que avisar): vuelve a permitir generar otra guia para esa entrega.
- Anular una Entrega exige que su guia (si existe) este anulada primero.
- No mueve stock (ya lo hizo la Entrega) ni llama a ningun servicio externo.

## API
`/api/guias-remision`: `POST desde-entrega/{entregaId}`, `PUT {id}/actualizar`, `GET listar`, `GET {id}`,
`PUT {id}/anular`.

## Frontend
Boton "Guía de remisión" en cada entrega de Pedidos de venta. Pantalla "Guías de remisión" (listar, ver,
anular) en el menu de Venta & Comprobante.

## Pruebas
Generar desde entrega copia los datos; no se genera dos veces para la misma entrega; anular libera la
entrega para generar otra vez; anular la entrega exige la guia anulada primero.
