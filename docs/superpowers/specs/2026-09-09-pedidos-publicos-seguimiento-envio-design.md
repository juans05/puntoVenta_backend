# Pedidos públicos con seguimiento de envío — design

Fecha: 2026-09-09
Estado: aprobado por el usuario en chat, pendiente de plan de implementación.

## Objetivo

Que el negocio (staff), tras acordar una venta por WhatsApp/redes, pueda enviarle
al cliente un link único donde este ingresa sus datos personales y de envío. Al
enviar el formulario se le crea automáticamente una cuenta (email + password
autogenerado, entregado por WhatsApp) desde la que puede rastrear el estado de
sus pedidos: un código de seguimiento si es envío a provincia, o el estado del
delivery si es Lima (local). El staff ve el pedido completo del lado
administrativo, lo despacha y puede imprimir una etiqueta de envío.

Inspirado en el flujo de "formulario público para recibir pedidos" de
Moradea.pe (competidor directo), adaptado a que **el staff ya conoce los
productos y el monto** (los acordó por chat) — el formulario público solo
captura identidad + ubicación, no un catálogo de autoservicio.

## Alcance

**Incluye:** entidad `Pedido` separada de la venta fiscal, cuenta de cliente
liviana (no ASP.NET Identity), formulario público de datos + envío, bandeja de
despacho para el staff, etiqueta de envío imprimible, portal de cliente
(login + lista/detalle de sus pedidos con tracking).

**No incluye (fuera de alcance, YAGNI):** integración real con APIs de
courier (Shalom/Olva) — el código de seguimiento es texto libre que el staff
copia de la guía física; pasarela de pago automática (Yape/Plin quedan como
métodos de pago que el staff registra manualmente, igual que hoy);
generación de PDF de etiquetas (se imprime la vista HTML con `window.print`);
catálogo de autoservicio para que el cliente elija productos.

## Decisiones confirmadas

| Pregunta | Decisión |
|---|---|
| ¿Dónde vive el pedido mientras el cliente llena sus datos? | Entidad nueva `Pedido`, separada de `ComprobanteCabecera` (más correcto que reutilizar el comprobante fiscal para algo pre-venta). |
| ¿Quién define productos/monto? | El staff, al generar el link (ya los acordó por chat). El formulario público NO incluye selección de productos. |
| ¿Cómo entra el cliente a ver su pedido después? | Cuenta propia: email + password autogenerado por el sistema, sin reusar el login de staff. |
| ¿Cómo se autentica esa cuenta? | Auth liviana y separada de ASP.NET Identity: tabla `ClienteCuenta` (1:1 con `Cliente`), password hasheado con `PasswordHasher<T>`, JWT propio con claim `clienteId`, sin roles ni permisos de RBAC interno. |
| ¿Cómo recibe el cliente su password? | Por WhatsApp, al celular que ingresó en el formulario. Requiere integrar envío saliente (no existe hoy — ver sección de riesgo). |
| Lima vs. provincia | Se deriva automáticamente de Departamento/Provincia que ingresa el cliente (`LIMA`+`LIMA` = local), no se digita a mano como hoy en `ComprobanteCabecera.TipoEnvio`. |

## Diseño

### 1. Entidad `Pedido` (nueva)

Hereda `EntityBase` (`Id`, `TenantId`, `FechaCreacion`, `UsuarioCreacion`,
`Estado` bool de activo/inactivo — no confundir con el estado de flujo del
pedido, que es un campo propio).

```
public class Pedido : EntityBase
{
    public int SucursalId { get; set; }
    public string Token { get; set; } = null!;          // único, va en la URL pública
    public char EstadoPedido { get; set; }               // ver EstatusPedido abajo
    public decimal Total { get; set; }

    // Cliente (llenado por el staff si ya lo conoce, o por el form público)
    public int? ClienteId { get; set; }
    public string? Nombre { get; set; }
    public string? Dni { get; set; }
    public string? Celular { get; set; }

    // Ubicación (llenado por el form público)
    public string? UbigeoId { get; set; }                // FK a Ubigeo (Departamento/Provincia/Distrito)
    public string? TipoEnvio { get; set; }                // "LOCAL" | "PROVINCIA", derivado
    public string? Direccion { get; set; }                // solo si LOCAL
    public string? Referencia { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    // Despacho
    public string? CodigoSeguimiento { get; set; }        // solo PROVINCIA, lo pone el staff
    public DateTime? FechaDespacho { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public bool PasswordEnviado { get; set; }             // false si falló el envío por WhatsApp

    // Venta final (se linkea cuando el staff factura, fuera de este proyecto el "cómo")
    public int? ComprobanteCabeceraId { get; set; }

    public Cliente? Cliente { get; set; }
    public Ubigeo? Ubigeo { get; set; }
    public ComprobanteCabecera? ComprobanteCabecera { get; set; }
    public List<PedidoDetalle> PedidoDetalles { get; set; } = new();
}

public static class EstatusPedido
{
    public const char Enviado = 'E';        // link generado, esperando que el cliente llene el form
    public const char DatosCompletos = 'D'; // cliente llenó el form
    public const char EnPreparacion = 'P';
    public const char Despachado = 'S';
    public const char EnCamino = 'C';       // solo LOCAL
    public const char Entregado = 'T';
    public const char Cancelado = 'A';
}
```

`PedidoDetalle` replica el patrón de `ComprobanteDetalle`: `PedidoId`,
`ProductoId`, `Cantidad`, `ValorUnitario`.

### 2. Entidad `ClienteCuenta` (nueva)

```
public class ClienteCuenta : EntityBase
{
    public int ClienteId { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public Cliente Cliente { get; set; } = null!;
}
```

Un `Cliente` puede existir sin `ClienteCuenta` (como hoy, para ventas
normales de mostrador). La cuenta solo se crea cuando el cliente pasa por el
flujo de pedido público.

### 3. Derivación de `TipoEnvio`

Al recibir el `UbigeoId` en el submit del formulario público:
`TipoEnvio = (ubigeo.Departamento == "LIMA" && ubigeo.Provincia == "LIMA") ? "LOCAL" : "PROVINCIA"`.

Simplificación deliberada: no se distingue Callao ni otras provincias del
departamento de Lima como "local" — quedan como `PROVINCIA` aunque en la
práctica el delivery pueda ser similar. Ajustable después si un negocio real
lo necesita.

### 4. Endpoints backend

**Staff (`[Authorize]`, dentro del esquema JWT actual):**
- `POST /api/pedidos` — crea el `Pedido` con ítems/monto, devuelve `Token` y
  la URL pública lista para copiar (`/pedido/{token}`).
- `GET /api/pedidos?estado=` — bandeja filtrable por estado.
- `GET /api/pedidos/{id}`
- `PUT /api/pedidos/{id}/estado` — transición de estado + `CodigoSeguimiento`
  cuando aplica.
- `POST /api/pedidos/{id}/reenviar-password` — reintento manual si
  `PasswordEnviado == false`.
- `GET /api/pedidos/{id}/etiqueta` — vista imprimible (HTML, no PDF).

**Público (`[AllowAnonymous]`):**
- `GET /api/pedidos/publico/{token}` — devuelve ítems/monto del pedido para
  mostrar en el form (sin exponer datos de otros clientes). 404 si el token
  no existe o el pedido ya está `Cancelado`.
- `POST /api/pedidos/publico/{token}` — recibe nombre/DNI/celular/ubigeo y,
  si es LOCAL, dirección/referencia o lat/lng. Idempotente: si el pedido ya
  está en `DatosCompletos` o posterior, devuelve 409 (evita doble registro
  por doble clic o recarga).
  1. Busca `Cliente` por DNI dentro del `TenantId`; si no existe, lo crea.
  2. Busca `ClienteCuenta` por `ClienteId`; si no existe, genera password
     aleatorio (8 caracteres), la crea con el hash, y guarda el password en
     claro **solo en memoria** para el paso 3 (nunca se persiste en claro).
  3. Llama a `IWhatsappOutboundService.EnviarTexto(celular, mensaje)` con el
     password. Si falla, `PasswordEnviado = false` pero el pedido queda
     `DatosCompletos` igual (el staff puede reenviar).
  4. Actualiza el `Pedido` (`TipoEnvio` derivado, `EstadoPedido = DatosCompletos`).

**Cliente (nuevo esquema de autorización `Policy = "Cliente"`, JWT propio):**
- `POST /api/cliente/login` — email + password → JWT con claim `clienteId`
  (sin roles).
- `GET /api/cliente/pedidos` — lista de pedidos de ese `ClienteId`, filtrada
  por el `TenantId` de la `ClienteCuenta` (igual que `Cliente`, la cuenta
  vive dentro de un solo negocio; si el mismo DNI compra en dos negocios
  distintos que usan PuntoVenta, son dos `Cliente`/`ClienteCuenta` separadas,
  una por tenant).
- `GET /api/cliente/pedidos/{id}` — detalle + estado/tracking. 403 si el
  pedido no es de ese cliente.

### 5. Envío saliente de WhatsApp (pieza nueva de infraestructura)

Hoy `WhatsappController`/`WhatsappService` solo procesan webhooks entrantes
de la Meta Cloud API y devuelven un texto de respuesta en el mismo ciclo del
webhook — no hay ningún llamado saliente a la Graph API
(`POST https://graph.facebook.com/{version}/{phone_number_id}/messages`).

Se agrega `IWhatsappOutboundService.EnviarTexto(numero, mensaje)` que hace
ese llamado con el `access_token`/`phone_number_id` del WABA ya usado para el
webhook entrante (a confirmar en el plan si esas credenciales ya están
disponibles como config o hay que gestionarlas).

**Riesgo de plataforma:** Meta solo permite mensajes de texto libre dentro de
una ventana de 24h desde el último mensaje del cliente; fuera de esa ventana
requiere una plantilla pre-aprobada (`message_template`). Como el flujo
normal es "el cliente escribe primero al negocio, el staff genera el link
casi de inmediato", debería caer dentro de la ventana en el caso típico. Si
falla (fuera de ventana, token vencido, número inválido), no se bloquea el
registro del pedido — se marca `PasswordEnviado = false` y el staff tiene un
botón de reenvío manual en la bandeja.

### 6. Frontend

**Staff** (nuevo módulo "Pedidos", junto a Ventas/Facturación):
- Pantalla "Nuevo pedido": reutiliza el selector de productos de
  `NuevaFactura` para armar ítems/monto → al guardar, muestra el link para
  copiar y pegar manualmente en la conversación de WhatsApp (el primer envío
  del link NO se automatiza, solo el password sí).
- Bandeja "Pedidos": lista filtrable por estado, acciones para avanzar de
  estado, poner código de seguimiento, reenviar password.
- Vista "Etiqueta de envío": HTML imprimible con nombre, dirección/distrito,
  celular, número de pedido — `window.print()`, sin librería de PDF.

**Público** (rutas sin auth, mismo patrón de rutas públicas que
self-service-tenant-signup):
- `/pedido/{token}` — formulario: nombre, DNI, celular, departamento/
  provincia/distrito (select en cascada usando `Ubigeo`), y si es Lima:
  dirección + referencia, o botón "usar mi ubicación actual" (geolocalización
  del navegador → lat/lng).
- `/mi-cuenta/login` — login del cliente.
- `/mi-cuenta/pedidos` — lista de pedidos con su estado; detalle muestra
  código de seguimiento (provincia) o línea de tiempo de estados (local).

### 7. Errores y casos borde

- Token inexistente/pedido cancelado → 404 amigable en la página pública, no
  un error genérico.
- Doble submit del formulario → 409, la página muestra "ya registraste tus
  datos para este pedido".
- Envío de WhatsApp falla → no bloquea el registro (ver sección 5).
- DNI ya asociado a un `Cliente` existente pero sin `ClienteCuenta` → se crea
  la cuenta sobre ese `Cliente` existente, no uno duplicado.
- Cliente que ya tiene `ClienteCuenta` y hace un pedido nuevo (otro `Token`)
  → no se genera password nuevo ni se reenvía WhatsApp, solo se linkea el
  `ClienteId` existente al nuevo `Pedido`.

### 8. Testing

- Unit: derivación de `TipoEnvio` (Lima+Lima vs. cualquier otra combinación),
  generación y verificación de password (hash), transición de estados de
  `Pedido` (no permitir saltos inválidos, ej. `Enviado` → `Entregado` directo).
- Integración: `POST /api/pedidos/publico/{token}` crea `Cliente` +
  `ClienteCuenta` + actualiza `Pedido` en una sola transacción; reintento con
  el mismo token después de `DatosCompletos` devuelve 409; `POST
  /api/cliente/login` devuelve JWT válido que en `GET /api/cliente/pedidos`
  solo trae pedidos de ese cliente (no de otros).
