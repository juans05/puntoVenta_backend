# Nueva Emisión de Factura — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rediseñar la emisión de venta (`NuevaFactura`) para capturar cliente completo (dirección/ubigeo/celular), permitir descuento/crédito/multipagos/vuelto reales, IGV y unidad de medida editables por línea, y tres modos de envío del comprobante a SUNAT, calcando el layout del mockup del usuario.

**Architecture:** Extiende `ComprobanteCabecera`/`ComprobanteDetalle` con las columnas nuevas (una migración EF). `ComprobanteRepository.CrearComprobante` gana la lógica de descuento/crédito/vuelto y, según `ModoEnvio`, guarda pendiente (comportamiento actual), envía síncrono via `IFacturacionProxy`, o marca `NoEnviar` (nunca se envía). Se agrega `ClienteRepository.ConsultarDocumento` para la lupa de búsqueda, con un bloque condicional (no una interfaz nueva) como punto de extensión para una API externa futura. El frontend reescribe `NuevaFactura/index.tsx` completo sobre el mismo patrón redux/axios ya usado.

**Tech Stack:** .NET 6 / EF Core (Npgsql) / AutoMapper / xUnit (backend). React + TypeScript + Redux (patrón `createReducer` con acciones string-literal) / CSS Modules (frontend).

**Spec:** `docs/superpowers/specs/2026-09-13-nueva-emision-factura-design.md`

## Global Constraints

- Toda columna nueva NOT NULL en una tabla con filas existentes necesita `.HasDefaultValue(...)` en su `Configuration` (si no, la migración falla al aplicarse sobre datos existentes).
- No se toca la firma de `IComprobanteRepository.CrearComprobante(ComprobantePayload)` ni `IComprobanteService.CrearComprobante` — el payload crece, pero el método sigue siendo el mismo.
- `ArmarInvoice`/`ArmarDetails` están duplicados hoy en `ComprobanteRepository.cs` (privado) y en `WEB_API/Jobs/InvoiceJob.cs` — es duplicación preexistente, fuera de alcance arreglarla; el cambio de `TipoIgv`/`UnidadMedida` se replica en **ambos** lugares.
- No se crea una interfaz `IConsultaDocumentoProxy` (el spec la mencionaba, pero es una interfaz para una sola implementación — se simplifica a un bloque condicional con comentario `ponytail:`, más fácil de extender después sin abstracción prematura).
- El frontend de este proyecto no tiene test suite (`Frontend/` no tiene Jest/Vitest configurado) — la verificación de UI es smoke manual en navegador, no tests automatizados.

---

### Task 1: Enums nuevos — `EstatusModoEnvio` y `EstatusEnvioSunat.NoEnviar`

**Files:**
- Modify: `Backend/Domain/Enumerations/Enums.cs`

**Interfaces:**
- Produces: `Domain.Enumerations.EstatusModoEnvio` (consts `SoloFirmarImprimir = 'F'`, `EnviarAhora = 'S'`, `SoloGuardar = 'G'`), `Domain.Enumerations.EstatusEnvioSunat.NoEnviar = 'N'` (nuevo miembro en la clase existente).

- [ ] **Step 1: Agregar el nuevo estado a `EstatusEnvioSunat` y la clase `EstatusModoEnvio`**

Editar `Backend/Domain/Enumerations/Enums.cs`, reemplazar el bloque:

```csharp
public static class EstatusEnvioSunat
{
    public const char Pendiente = 'P';
    public const char Enviado = 'E';
    public const char Error = 'X';
}
```

por:

```csharp
public static class EstatusEnvioSunat
{
    public const char Pendiente = 'P';
    public const char Enviado = 'E';
    public const char Error = 'X';
    public const char NoEnviar = 'N'; // "Solo Firmar e Imprimir": nunca pasa por el job de SUNAT
}

public static class EstatusModoEnvio
{
    public const char SoloFirmarImprimir = 'F';
    public const char EnviarAhora = 'S';
    public const char SoloGuardar = 'G';
}
```

- [ ] **Step 2: Compilar para verificar que no rompe nada**

Run: `cd Backend && dotnet build WEB_API_SPA.sln`
Expected: `0 Errores` (los mismos warnings preexistentes están bien).

- [ ] **Step 3: Commit**

```bash
git add Backend/Domain/Enumerations/Enums.cs
git commit -m "feat: add EstatusModoEnvio and EstatusEnvioSunat.NoEnviar enums"
```

---

### Task 2: Columnas nuevas en `ComprobanteCabecera` y `ComprobanteDetalle` + migración

**Files:**
- Modify: `Backend/Domain/Entities/ComprobanteCabecera.cs`
- Modify: `Backend/Domain/Entities/ComprobanteDetalle.cs`
- Modify: `Backend/Infrastructure/Configuration/ComprobanteCabeceraConfiguration.cs`
- Modify: `Backend/Infrastructure/Configuration/ComprobanteDetalleConfiguration.cs`
- Create: migración generada por `dotnet ef migrations add` (nombre exacto se ve en el Step 4)

**Interfaces:**
- Consumes: `Domain.Enumerations.EstatusModoEnvio.SoloGuardar` (Task 1).
- Produces: `ComprobanteCabecera.{Direccion, UbigeoId, Celular, EnviarComprobanteEmail, EsCredito, DescuentoPorcentaje, DescuentoTotal, TotalRecibido, Vuelto, Observacion, ModoEnvio, Ubigeo}` y `ComprobanteDetalle.{TipoIgv, UnidadMedida}` — usados por Task 3, 4, 5, 6.

- [ ] **Step 1: Agregar las propiedades a `ComprobanteCabecera`**

En `Backend/Domain/Entities/ComprobanteCabecera.cs`, agregar después de `public string? Distrito { get; set; }` (línea 31) y antes de `public int? ComprobanteAfectadoId { get; set; }`:

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
```

Y agregar la navegación, junto a las otras (`public Cliente? Cliente { get; set; }` etc.):

```csharp
    public Ubigeo? Ubigeo { get; set; }
```

El archivo ya tiene `using Domain.Enumerations;` en la línea 1, así que `EstatusModoEnvio` se resuelve sin agregar un `using` nuevo.

- [ ] **Step 2: Agregar las propiedades a `ComprobanteDetalle`**

En `Backend/Domain/Entities/ComprobanteDetalle.cs`, agregar después de `public decimal? CostoReal { get; set; }` (línea 21):

```csharp
        public string TipoIgv { get; set; } = "10";      // codigo SUNAT: 10 Gravado, 20 Exonerado, 30 Inafecto
        public string UnidadMedida { get; set; } = "NIU"; // codigo SUNAT: NIU Unidad, ZZ Servicio, KGM Kilogramo
```

- [ ] **Step 3: Configurar las columnas nuevas en EF (defaults y FK)**

En `Backend/Infrastructure/Configuration/ComprobanteCabeceraConfiguration.cs`, agregar antes del cierre del constructor (antes de la línea `entityBuilder.HasOne(u => u.MotivoNota)...` está bien, o al final, cualquier posición dentro del constructor funciona — agregarlo justo después del bloque `entityBuilder.Property(u => u.Distrito)...HasMaxLength(100);`):

```csharp
        entityBuilder.Property(u => u.Direccion)
                     .HasMaxLength(250);

        entityBuilder.Property(u => u.UbigeoId)
                     .HasMaxLength(10);

        entityBuilder.Property(u => u.Celular)
                     .HasMaxLength(20);

        entityBuilder.Property(u => u.EnviarComprobanteEmail)
                     .HasDefaultValue(false);

        entityBuilder.Property(u => u.EsCredito)
                     .HasDefaultValue(false);

        entityBuilder.Property(u => u.Observacion)
                     .HasMaxLength(500);

        entityBuilder.Property(u => u.ModoEnvio)
                     .IsRequired()
                     .HasMaxLength(1)
                     .HasDefaultValue(EstatusModoEnvio.SoloGuardar);

        entityBuilder.HasOne(u => u.Ubigeo)
                     .WithMany()
                     .HasForeignKey(u => u.UbigeoId)
                     .IsRequired(false)
                     .OnDelete(DeleteBehavior.Restrict);
```

Agregar `using Domain.Enumerations;` al inicio del archivo (junto a `using Domain.Entities;`).

En `Backend/Infrastructure/Configuration/ComprobanteDetalleConfiguration.cs`, agregar dentro del constructor, después de `entityBuilder.Property(e => e.ValorUnitario).HasColumnType("decimal(13,2)");`:

```csharp
            entityBuilder.Property(e => e.TipoIgv)
                         .IsRequired()
                         .HasMaxLength(2)
                         .HasDefaultValue("10");

            entityBuilder.Property(e => e.UnidadMedida)
                         .IsRequired()
                         .HasMaxLength(3)
                         .HasDefaultValue("NIU");
```

- [ ] **Step 4: Generar la migración**

Run: `cd Backend && dotnet ef migrations add AddNuevaEmisionFacturaFields --project Infrastructure --startup-project WEB_API`
Expected: `Done. To undo this action, use 'ef migrations remove'` y dos archivos nuevos bajo `Backend/Infrastructure/Migrations/` (`<timestamp>_AddNuevaEmisionFacturaFields.cs` y su `.Designer.cs`), más `SpaContextModelSnapshot.cs` modificado.

Abrir el `.cs` generado (no el `.Designer.cs`) y confirmar que `Up()` tiene un `AddColumn` por cada propiedad nueva de ambas tablas, un `AddForeignKey`/`CreateIndex` para `ComprobanteCabecera.UbigeoId → Ubigeo`, y que las columnas `bool`/`ModoEnvio`/`TipoIgv`/`UnidadMedida` traen `defaultValue:` (no deben quedar sin default, o el `dotnet ef database update` fallará contra una tabla con filas).

- [ ] **Step 5: Compilar**

Run: `cd Backend && dotnet build WEB_API_SPA.sln`
Expected: `0 Errores`.

- [ ] **Step 6: Commit**

```bash
git add Backend/Domain/Entities/ComprobanteCabecera.cs Backend/Domain/Entities/ComprobanteDetalle.cs \
        Backend/Infrastructure/Configuration/ComprobanteCabeceraConfiguration.cs \
        Backend/Infrastructure/Configuration/ComprobanteDetalleConfiguration.cs \
        Backend/Infrastructure/Migrations/
git commit -m "feat: add cliente/descuento/credito/envio fields to ComprobanteCabecera and TipoIgv/UnidadMedida to ComprobanteDetalle"
```

---

### Task 3: Payload — nuevos campos en `ComprobantePayload`/`ComprobanteDetallePayload`

**Files:**
- Modify: `Backend/Domain/Payloads/ComprobantePayload.cs`

**Interfaces:**
- Consumes: `Domain.Enumerations.EstatusModoEnvio.SoloGuardar` (Task 1).
- Produces: `ComprobantePayload.{Direccion, UbigeoId, Celular, EnviarComprobanteEmail, EsCredito, DescuentoPorcentaje, TotalRecibido, Observacion, ModoEnvio}` y `ComprobanteDetallePayload.{TipoIgv, UnidadMedida}` — consumidos por Task 4/5/6.

- [ ] **Step 1: Agregar los campos**

En `Backend/Domain/Payloads/ComprobantePayload.cs`, la clase `ComprobantePayload` queda:

```csharp
using Domain.Enumerations;

namespace Domain.Payloads
{
    public class ComprobantePayload
    {
        public int? ClienteId { get; set; }
        public int TipoDocumentoVentaId { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? RazonSocial { get; set; }

        public decimal Total { get; set; }
        public DateTime? FechaVenta { get; set; }
        public bool? EsEcommerce { get; set; }
        public string? TipoEnvio { get; set; }
        public string? Distrito { get; set; }
        public string? Direccion { get; set; }
        public string? UbigeoId { get; set; }
        public string? Celular { get; set; }
        public bool EnviarComprobanteEmail { get; set; }
        public bool EsCredito { get; set; }
        public decimal? DescuentoPorcentaje { get; set; }
        public decimal? TotalRecibido { get; set; }
        public string? Observacion { get; set; }
        public char ModoEnvio { get; set; } = EstatusModoEnvio.SoloGuardar;
        public List<ComprobanteDetallePayload> DetalleComprobante { get; set; } = new List<ComprobanteDetallePayload>();
        public List<PagoPayload> DetallePago { get; set; } = new List<PagoPayload>();

    }

    public class ComprobanteDetallePayload
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal? CostoReal { get; set; }
        public string TipoIgv { get; set; } = "10";
        public string UnidadMedida { get; set; } = "NIU";
    }

    public class PagoPayload
    {
        public int MetodoPagoId { get; set; }
        public decimal Monto { get; set; }
    }
}
```

(`CreateMap<ComprobantePayload, ComprobanteCabecera>()` y `CreateMap<ComprobanteDetallePayload, ComprobanteDetalle>()` en `Backend/Domain/Common/Mappings/MyAutomapper.cs` ya existen sin `.ForMember`, así que AutoMapper mapea estos campos nuevos automáticamente por nombre — no hay que tocar ese archivo.)

- [ ] **Step 2: Compilar**

Run: `cd Backend && dotnet build WEB_API_SPA.sln`
Expected: `0 Errores`.

- [ ] **Step 3: Commit**

```bash
git add Backend/Domain/Payloads/ComprobantePayload.cs
git commit -m "feat: add cliente/descuento/credito/envio fields to ComprobantePayload"
```

---

### Task 4: `CrearComprobante` — descuento, crédito y validación de vuelto

**Files:**
- Modify: `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`
- Test: `Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs`

**Interfaces:**
- Consumes: `ComprobantePayload.{EsCredito, DescuentoPorcentaje, TotalRecibido}` (Task 3), campos nuevos de `ComprobanteCabecera` (Task 2).
- Produces: `ComprobanteCabecera.{DescuentoTotal, TotalRecibido, Vuelto}` calculados y persistidos — no los consume ninguna otra tarea directamente, pero Task 5 modifica el mismo método.

- [ ] **Step 1: Escribir los tests que fallan**

Agregar a `Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs`, dentro de la clase `ComprobanteRepositoryTests`, después del método `SeedMetodoPagoAsync`:

```csharp
    [Fact]
    public async Task CrearComprobante_ConDescuentoPorcentaje_AplicaElDescuentoAlTotal()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        // Detalle suma 100; 10% de descuento -> total esperado 90.
        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 90m,
            DescuentoPorcentaje = 10m,
            TotalRecibido = 90m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 1, ValorUnitario = 100m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 90m }
            }
        };

        var (estado, _, message) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(90m, cabecera.ValorTotal);
        Assert.Equal(10m, cabecera.DescuentoTotal);
        Assert.Equal(0m, cabecera.Vuelto);
    }

    [Fact]
    public async Task CrearComprobante_EsCredito_NoExigeTotalRecibido()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            EsCredito = true,
            TotalRecibido = null,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        var (estado, _, message) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.True(cabecera.EsCredito);
        Assert.Null(cabecera.TotalRecibido);
        Assert.Null(cabecera.Vuelto);
    }

    [Fact]
    public async Task CrearComprobante_NoCreditoConRecibidoMenorAlTotal_FallaValidacion()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            EsCredito = false,
            TotalRecibido = 15m, // menor al total
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        var (estado, _, message) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.False(await context.ComprobanteCabecera.AnyAsync());
    }

    [Fact]
    public async Task CrearComprobante_SumaDePagosNoCoincideConElTotal_FallaValidacion()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            TotalRecibido = 20m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                // Multipagos: dos lineas que suman 15, no 20 -> debe fallar.
                new() { MetodoPagoId = metodoPagoId, Monto = 10m },
                new() { MetodoPagoId = metodoPagoId, Monto = 5m }
            }
        };

        var (estado, _, message) = await repo.CrearComprobante(payload);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.False(await context.ComprobanteCabecera.AnyAsync());
    }
```

- [ ] **Step 2: Correr los tests para confirmar que fallan**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~CrearComprobante_ConDescuentoPorcentaje|FullyQualifiedName~CrearComprobante_EsCredito|FullyQualifiedName~CrearComprobante_NoCreditoConRecibidoMenorAlTotal|FullyQualifiedName~CrearComprobante_SumaDePagosNoCoincideConElTotal"`
Expected: los 4 `FAIL` — el de descuento y el de crédito porque `payload.Total` (90/20) no coincide con `sumatoria` sin descontar (100/20 sí coincide para crédito así que ese en particular podría pasar de casualidad — igual, correr para confirmar el estado real antes de tocar código), el de "recibido menor" porque hoy no existe ninguna validación de vuelto (pasaría `Ok` en vez de `FailedValidation`), y el de "suma de pagos" porque hoy no existe ninguna validación de `DetallePago` contra el total (pasaría `Ok`).

- [ ] **Step 3: Implementar la lógica de descuento/crédito/vuelto**

En `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`, dentro de `CrearComprobante`, reemplazar:

```csharp
                var sumatoria = payload.DetalleComprobante.Sum(q => q.ValorUnitario * q.Cantidad);

                // El total llega desde JS (suma en punto flotante, ej. 121.80000000000001) --
                // se redondea a centimos antes de comparar contra la suma exacta en decimal.
                if (Math.Round(payload.Total, 2) != Math.Round(sumatoria, 2))
                    return (ServiceStatus.FailedValidation, null, "El total no coincide con la suma de los detalles");
```

por:

```csharp
                var sumatoria = payload.DetalleComprobante.Sum(q => q.ValorUnitario * q.Cantidad);

                var descuentoTotal = Math.Round(sumatoria * (payload.DescuentoPorcentaje ?? 0m) / 100m, 2);
                var totalEsperado = sumatoria - descuentoTotal;

                // El total llega desde JS (suma en punto flotante, ej. 121.80000000000001) --
                // se redondea a centimos antes de comparar contra el total esperado (detalle - descuento).
                if (Math.Round(payload.Total, 2) != Math.Round(totalEsperado, 2))
                    return (ServiceStatus.FailedValidation, null, "El total no coincide con la suma de los detalles menos el descuento");

                if (!payload.EsCredito)
                {
                    if (!payload.TotalRecibido.HasValue || Math.Round(payload.TotalRecibido.Value, 2) < Math.Round(totalEsperado, 2))
                        return (ServiceStatus.FailedValidation, null, "El monto recibido es menor al total");
                }

                // Multipagos: la suma de las lineas de DetallePago debe calzar con el total de la
                // venta (misma tolerancia de redondeo a centimos que la comparacion de arriba).
                var sumaPagos = payload.DetallePago.Sum(p => p.Monto);
                if (Math.Round(sumaPagos, 2) != Math.Round(totalEsperado, 2))
                    return (ServiceStatus.FailedValidation, null, "La suma de los pagos no coincide con el total");
```

Y, más abajo, donde se arma `cabecera` (después de `cabecera.TotalLetras = DecimalExtensions.ConvertirNumeroALetras(payload.Total);`), agregar:

```csharp
                cabecera.DescuentoTotal = descuentoTotal;
                cabecera.TotalRecibido = payload.EsCredito ? null : payload.TotalRecibido;
                cabecera.Vuelto = payload.EsCredito ? null : Math.Round((payload.TotalRecibido!.Value - payload.Total), 2);
```

(`cabecera.EsCredito`, `cabecera.DescuentoPorcentaje`, `cabecera.Direccion`, `cabecera.UbigeoId`, `cabecera.Celular`, `cabecera.EnviarComprobanteEmail`, `cabecera.Observacion` ya llegan solos vía `_mapper.Map<ComprobanteCabecera>(payload)` — no hace falta asignarlos a mano.)

- [ ] **Step 4: Correr los tests para confirmar que pasan**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~CrearComprobante_ConDescuentoPorcentaje|FullyQualifiedName~CrearComprobante_EsCredito|FullyQualifiedName~CrearComprobante_NoCreditoConRecibidoMenorAlTotal|FullyQualifiedName~CrearComprobante_SumaDePagosNoCoincideConElTotal"`
Expected: los 4 `PASS`.

- [ ] **Step 5: Correr toda la suite para verificar que no rompió nada**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj`
Expected: todos los tests (los preexistentes + los 3 nuevos) en verde. Si `CrearComprobante_SinSeriecorrelativoPrevio_LoCreaYArrancaEnUno` o `CrearComprobante_DosVentasSeguidas_IncrementaElMismoCorrelativo` fallan por la nueva validación de `TotalRecibido`, es porque esos payloads no lo setean — actualizarlos agregando `TotalRecibido = payload.Total` (o el valor total de esa venta) a los `ComprobantePayload` de esos dos tests existentes.

- [ ] **Step 6: Commit**

```bash
git add Backend/Infrastructure/Repositories/ComprobanteRepository.cs Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs
git commit -m "feat: apply discount and validate change (vuelto) in CrearComprobante"
```

---

### Task 5: `CrearComprobante` — modo de envío (guardar / enviar ahora / firmar e imprimir)

**Files:**
- Modify: `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`
- Test: `Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs`

**Interfaces:**
- Consumes: `ComprobantePayload.ModoEnvio` (Task 3), `EstatusModoEnvio`/`EstatusEnvioSunat.NoEnviar` (Task 1), `IFacturacionProxy.EnviarComprobanteSunar<T>(InvoiceRequest, string?)` (ya existe en `Backend/Application/Interfaces/IProxies/IFacturacionProxy.cs`), el método privado `ArmarInvoice` ya existente en este mismo archivo.
- Produces: `ComprobanteCabecera.EnviadoSunat` reflejando el modo elegido — lo verifica Task 6 indirectamente (no lo consume código nuevo).

- [ ] **Step 1: Escribir un fake de `IFacturacionProxy` para tests y los tests que fallan**

Agregar un fake al final de `Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs`, fuera de la clase `ComprobanteRepositoryTests` (después de su llave de cierre):

```csharp
public class FakeFacturacionProxy : Application.Interfaces.IProxies.IFacturacionProxy
{
    public bool DeberiaFallar { get; set; }

    public Task<T> EnviarComprobanteSunar<T>(Domain.Models.InvoiceRequest cabecera, string? accessToken = null)
    {
        var respuesta = new Domain.Models.InvoiceResponse
        {
            sunatResponse = new Domain.Models.SunatResponse
            {
                success = !DeberiaFallar,
                error = DeberiaFallar ? new Domain.Models.Error { code = "1", message = "Fallo simulado" } : null,
                cdrResponse = new Domain.Models.CdrResponse { notes = new List<object>() }
            }
        };
        return Task.FromResult((T)(object)respuesta);
    }

    public Task<T> EnviarNotaSunar<T>(Domain.Models.NoteRequest cabecera, string? accessToken = null) => throw new NotImplementedException();
    public Task<T> ResumenAnulacion<T>(Domain.Models.SummaryRequest cabecera, string? accessToken = null) => throw new NotImplementedException();
    public Task<T> GenerarPdf<T>(Domain.Models.InvoiceRequest cabecera, string? accessToken = null) => throw new NotImplementedException();
}
```

Antes de escribir el fake, leer `Backend/Application/Interfaces/IProxies/IFacturacionProxy.cs` para confirmar la firma exacta de sus 4 métodos y ajustar el fake si difiere de lo mostrado arriba (fue reconstruida a partir del uso en `Backend/Application/Proxies/FacturacionProxy.cs`, no leída directamente).

Agregar los tests, dentro de la clase `ComprobanteRepositoryTests`:

```csharp
    [Fact]
    public async Task CrearComprobante_ModoSoloFirmarImprimir_QuedaComoNoEnviarYFueraDelJob()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory(), new FakeFacturacionProxy());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            TotalRecibido = 20m,
            ModoEnvio = EstatusModoEnvio.SoloFirmarImprimir,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        var (estado, _, _) = await repo.CrearComprobante(payload);
        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(EstatusEnvioSunat.NoEnviar, cabecera.EnviadoSunat);

        var (_, pendientes) = await repo.ListarComprobantesPendientesEnviarSunat(cabecera.TenantId);
        Assert.True(pendientes == null || pendientes.Count == 0);
    }

    [Fact]
    public async Task CrearComprobante_ModoEnviarAhora_LlamaAlProxySincronoYQuedaEnviado()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory(), new FakeFacturacionProxy());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            TotalRecibido = 20m,
            ModoEnvio = EstatusModoEnvio.EnviarAhora,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        var (estado, _, _) = await repo.CrearComprobante(payload);
        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(EstatusEnvioSunat.Enviado, cabecera.EnviadoSunat);
    }

    [Fact]
    public async Task CrearComprobante_ModoSoloGuardar_QuedaPendienteComoHoy()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory(), new FakeFacturacionProxy());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            TotalRecibido = 20m,
            ModoEnvio = EstatusModoEnvio.SoloGuardar,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        var (estado, _, _) = await repo.CrearComprobante(payload);
        Assert.Equal(ServiceStatus.Ok, estado);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();
        Assert.Equal(EstatusEnvioSunat.Pendiente, cabecera.EnviadoSunat);
    }
```

- [ ] **Step 2: Correr los tests para confirmar que fallan**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ModoSoloFirmarImprimir|FullyQualifiedName~ModoEnviarAhora|FullyQualifiedName~ModoSoloGuardar"`
Expected: `FAIL` en los 3 — el constructor `new ComprobanteRepository(..., new FakeFacturacionProxy())` ni siquiera compila todavía (5 argumentos contra los 4 actuales). Confirmar que el error de compilación es exactamente ese antes de seguir.

- [ ] **Step 3: Inyectar `IFacturacionProxy` y aplicar el modo de envío**

En `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`, agregar el using y el campo/constructor:

```csharp
using Application.Interfaces.IProxies;
```

(agregar junto a los demás `using` del inicio del archivo).

Reemplazar el constructor:

```csharp
        private readonly SpaContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly TaxCalculatorFactory _taxCalculatorFactory;


        public ComprobanteRepository(
            SpaContext context,
            IMapper mapper,
            IHttpContextAccessor? httpContextAccessor,
            TaxCalculatorFactory taxCalculatorFactory)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _taxCalculatorFactory = taxCalculatorFactory;
        }
```

por:

```csharp
        private readonly SpaContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly TaxCalculatorFactory _taxCalculatorFactory;
        private readonly IFacturacionProxy? _facturacionProxy;


        public ComprobanteRepository(
            SpaContext context,
            IMapper mapper,
            IHttpContextAccessor? httpContextAccessor,
            TaxCalculatorFactory taxCalculatorFactory,
            IFacturacionProxy? facturacionProxy = null)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _taxCalculatorFactory = taxCalculatorFactory;
            _facturacionProxy = facturacionProxy;
        }
```

(el parámetro nuevo es opcional con default `null` a propósito: los 2 tests preexistentes que instancian `ComprobanteRepository` con 4 argumentos posicionales — `CrearComprobante_SinSeriecorrelativoPrevio_LoCreaYArrancaEnUno`, `CrearComprobante_DosVentasSeguidas_IncrementaElMismoCorrelativo`, más los 2 de `AnularVenta` y el de `ListarComprobantes` — siguen compilando sin tocarlos; en producción, `IFacturacionProxy` ya está registrado como `AddScoped` en `Backend/Infrastructure/DependencyInjection.cs:81`, así que .NET lo inyecta solo).

Dentro de `CrearComprobante`, justo antes de `if (payload.TipoDocumentoVentaId == 1) cabecera.Serie = ...`, agregar:

```csharp
                // cabecera.ModoEnvio ya llego mapeado desde payload.ModoEnvio (AutoMapper, mismo
                // nombre en ambos lados) -- solo falta derivar EnviadoSunat a partir de ese valor.
                cabecera.EnviadoSunat = cabecera.ModoEnvio == EstatusModoEnvio.SoloFirmarImprimir
                    ? EstatusEnvioSunat.NoEnviar
                    : EstatusEnvioSunat.Pendiente;
```

Y, justo antes de `await _context.Database.CommitTransactionAsync();`, sin sacarlo de dentro del `try`, agregar el envío síncrono (después del bloque que guarda `pagos` y su `SaveChangesAsync`):

```csharp
                if (cabecera.ModoEnvio == EstatusModoEnvio.EnviarAhora && _facturacionProxy != null)
                {
                    var invoiceRequest = ArmarInvoice(cabecera, config);
                    var respuesta = await _facturacionProxy.EnviarComprobanteSunar<Domain.Models.InvoiceResponse>(invoiceRequest, config?.Token);

                    if (respuesta.sunatResponse.success)
                    {
                        cabecera.EnviadoSunat = EstatusEnvioSunat.Enviado;
                        cabecera.MensajeSunat = System.Text.Json.JsonSerializer.Serialize(respuesta.sunatResponse.cdrResponse?.notes);
                    }
                    else
                    {
                        cabecera.EnviadoSunat = EstatusEnvioSunat.Error;
                        cabecera.MensajeSunat = respuesta.sunatResponse.error?.message;
                    }

                    await _context.SaveChangesAsync();
                }
```

`ArmarInvoice` necesita que `cabecera.ComprobanteDetalles` esté cargado con sus `Producto` (lo usa `ArmarDetails` vía `x.Producto.Nombre`) — como `detalle` se agregó recién con `AddRangeAsync` en el mismo `_context` y cada `item.Producto` no quedó seteado explícitamente, agregar justo antes de llamar `ArmarInvoice` (o reemplazar el `cabecera.ComprobanteDetalles` en memoria):

```csharp
                cabecera.ComprobanteDetalles = detalle;
```

(esto va inmediatamente después de `await _context.ComprobanteDetalle.AddRangeAsync(detalle);` y antes del bloque de `pagos` — la propiedad `Producto` de cada `ComprobanteDetalle` en `detalle` ya está poblada porque el `foreach` de más arriba hizo `var producto = await _context.Producto...` pero no la asignó a `item.Producto`; agregar `item.Producto = producto;` dentro de ese mismo `foreach`, justo después de `producto.Stock = stockAnterior - item.Cantidad;`).

- [ ] **Step 4: Correr los tests para confirmar que pasan**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ModoSoloFirmarImprimir|FullyQualifiedName~ModoEnviarAhora|FullyQualifiedName~ModoSoloGuardar"`
Expected: los 3 `PASS`.

- [ ] **Step 5: Correr toda la suite**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj`
Expected: todo en verde.

- [ ] **Step 6: Commit**

```bash
git add Backend/Infrastructure/Repositories/ComprobanteRepository.cs Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs
git commit -m "feat: add ModoEnvio handling to CrearComprobante (guardar/enviar ahora/firmar e imprimir)"
```

---

### Task 6: IGV y unidad de medida por línea (dejar de hardcodear "NIU"/"10")

**Files:**
- Modify: `Backend/Infrastructure/Repositories/ComprobanteRepository.cs` (método privado `ArmarInvoice`)
- Modify: `Backend/WEB_API/Jobs/InvoiceJob.cs` (método privado `ArmarDetails`)
- Test: `Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs`

**Interfaces:**
- Consumes: `ComprobanteDetalle.{TipoIgv, UnidadMedida}` (Task 2).

- [ ] **Step 1: Escribir el test que falla**

Agregar a `Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs`. Este test necesita `GeneratePdfRequest` (ya existe en `IComprobanteRepository`, arma un `InvoiceRequest` sin enviarlo — confirmar su firma exacta leyendo `Backend/Infrastructure/Repositories/ComprobanteRepository.cs` antes de escribir el test si el nombre difiere):

```csharp
    [Fact]
    public async Task ArmarInvoice_UsaTipoIgvYUnidadMedidaDeCadaLinea_NoElValorHardcodeado()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        await SeedTipoDocumentoVentaAsync(context);
        var productoId = await SeedProductoAsync(context, stock: 10);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var repo = new ComprobanteRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null, new Application.Abstractions.TaxCalculatorFactory());

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = 2,
            Total = 20m,
            TotalRecibido = 20m,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = productoId, Cantidad = 2, ValorUnitario = 10m, TipoIgv = "20", UnidadMedida = "ZZ" }
            },
            DetallePago = new List<PagoPayload>
            {
                new() { MetodoPagoId = metodoPagoId, Monto = 20m }
            }
        };

        await repo.CrearComprobante(payload);

        var cabecera = await context.ComprobanteCabecera.SingleAsync();

        var (estado, invoiceRequest, _) = await repo.GeneratePdfRequest(cabecera.Id);

        Assert.Equal(ServiceStatus.Ok, estado);
        var linea = Assert.Single(invoiceRequest!.details);
        Assert.Equal("20", linea.tipAfeIgv);
        Assert.Equal("ZZ", linea.unidad);
    }
```

- [ ] **Step 2: Correr el test para confirmar que falla**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ArmarInvoice_UsaTipoIgvYUnidadMedida"`
Expected: `FAIL` — `linea.tipAfeIgv` es `"10"` y `linea.unidad` es `"NIU"` (los valores hardcodeados), no `"20"`/`"ZZ"`.

- [ ] **Step 3: Usar los campos de la línea en vez de los literales**

En `Backend/Infrastructure/Repositories/ComprobanteRepository.cs`, dentro del método privado `ArmarInvoice` (cerca del final del archivo), reemplazar:

```csharp
                details = comprobanteCabecera.ComprobanteDetalles.Select(x => new Detail
                {
                    unidad = "NIU",
                    codProducto = "P001",
                    cantidad = x.Cantidad,
                    descripcion = x.Producto.Nombre,
                    mtoValorUnitario = Math.Round(x.ValorUnitario / factor, 2),
                    mtoValorVenta = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                    mtoBaseIgv = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                    porcentajeIgv = impuesto,
                    igv = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                    tipAfeIgv = "10",
                    totalImpuestos = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                    mtoPrecioUnitario = x.ValorUnitario,
                }).ToList(),
```

por:

```csharp
                details = comprobanteCabecera.ComprobanteDetalles.Select(x => new Detail
                {
                    unidad = x.UnidadMedida,
                    codProducto = "P001",
                    cantidad = x.Cantidad,
                    descripcion = x.Producto.Nombre,
                    mtoValorUnitario = Math.Round(x.ValorUnitario / factor, 2),
                    mtoValorVenta = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                    mtoBaseIgv = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                    porcentajeIgv = impuesto,
                    igv = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                    tipAfeIgv = x.TipoIgv,
                    totalImpuestos = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                    mtoPrecioUnitario = x.ValorUnitario,
                }).ToList(),
```

En `Backend/WEB_API/Jobs/InvoiceJob.cs`, dentro del método privado `ArmarDetails`, aplicar el mismo cambio: reemplazar `unidad = "NIU",` por `unidad = x.UnidadMedida,` y `tipAfeIgv = "10",` por `tipAfeIgv = x.TipoIgv,`.

- [ ] **Step 4: Correr el test para confirmar que pasa**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ArmarInvoice_UsaTipoIgvYUnidadMedida"`
Expected: `PASS`.

- [ ] **Step 5: Correr toda la suite y compilar el WEB_API (por el cambio en `InvoiceJob.cs`)**

Run: `cd Backend && dotnet build WEB_API_SPA.sln && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj`
Expected: `0 Errores` y todos los tests en verde.

- [ ] **Step 6: Commit**

```bash
git add Backend/Infrastructure/Repositories/ComprobanteRepository.cs Backend/WEB_API/Jobs/InvoiceJob.cs Backend/Tests/Infrastructure.Tests/ComprobanteRepositoryTests.cs
git commit -m "fix: use per-line TipoIgv/UnidadMedida instead of hardcoded NIU/10 in ArmarInvoice"
```

---

### Task 7: Búsqueda de cliente por documento (la lupa)

**Files:**
- Modify: `Backend/Application/Interfaces/IRepository/IClienteRepository.cs`
- Modify: `Backend/Application/Interfaces/IServices/IClienteService.cs`
- Modify: `Backend/Infrastructure/Repositories/ClienteRepository.cs`
- Modify: `Backend/Application/Services/ClienteService.cs`
- Modify: `Backend/WEB_API/Controllers/ClientesController.cs`
- Test: crear `Backend/Tests/Infrastructure.Tests/ClienteRepositoryTests.cs` (no existe hoy)

**Interfaces:**
- Produces: `GET /api/clientes/consultar-documento?tipoDoc=&numero=` → `ClienteDto?` — lo consume el frontend en Task 9.

- [ ] **Step 1: Escribir el test que falla**

Antes de escribir el repositorio, leer `Backend/Domain/DTO/ClienteDto.cs` y `Backend/Domain/Common/Mappings/MyAutomapper.cs` (ya se leyeron durante la planificación: `CreateMap<Cliente, ClienteDto>()` existe con `.ForMember` para `Sexo`/`FechaNacimiento`, mapea `Nombre`/`NumeroDocumento`/`Direccion`/`Telefono`/`UbigeoId`/`Ubigeo` por convención).

Crear `Backend/Tests/Infrastructure.Tests/ClienteRepositoryTests.cs`:

```csharp
using Domain.Entities;
using Domain.Models;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Infrastructure.Tests;

public class ClienteRepositoryTests
{
    private static IConfiguration SinTokenConfigurado()
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
    }

    [Fact]
    public async Task ConsultarDocumento_ClienteExistente_LoDevuelve()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        context.Cliente.Add(new Cliente { Nombre = "Juan Perez", NumeroDocumento = "12345678", TipoDocumentoId = 1 });
        await context.SaveChangesAsync();

        var repo = new ClienteRepository(context, TestDbContextFactory.Mapper, SinTokenConfigurado());

        var (estado, resultado, _) = await repo.ConsultarDocumento("dni", "12345678");

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.NotNull(resultado);
        Assert.Equal("Juan Perez", resultado!.Nombre);
    }

    [Fact]
    public async Task ConsultarDocumento_SinMatchYSinToken_DevuelveNullSinError()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        using var _ = connection;

        var repo = new ClienteRepository(context, TestDbContextFactory.Mapper, SinTokenConfigurado());

        var (estado, resultado, _) = await repo.ConsultarDocumento("dni", "99999999");

        Assert.Equal(ServiceStatus.Ok, estado);
        Assert.Null(resultado);
    }
}
```

- [ ] **Step 2: Correr el test para confirmar que falla**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ClienteRepositoryTests"`
Expected: error de compilación (`ConsultarDocumento` no existe todavía en `ClienteRepository`, y el constructor no acepta `IConfiguration`).

- [ ] **Step 3: Implementar `ConsultarDocumento`**

En `Backend/Application/Interfaces/IRepository/IClienteRepository.cs`, agregar al final de la interfaz:

```csharp
        Task<(ServiceStatus, ClienteDto?, string)> ConsultarDocumento(string tipoDoc, string numero);
```

En `Backend/Application/Interfaces/IServices/IClienteService.cs`, agregar:

```csharp
        Task<MessageResult<object>> ConsultarDocumento(string tipoDoc, string numero);
```

En `Backend/Infrastructure/Repositories/ClienteRepository.cs`, agregar `using Microsoft.Extensions.Configuration;` al inicio, cambiar el constructor:

```csharp
public class ClienteRepository :  IClienteRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;
    private readonly IConfiguration configuration;

    public ClienteRepository(SpaContext dbContext, IMapper mapper, IConfiguration configuration)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        this.configuration = configuration;
    }
```

y agregar el método (al final de la clase, antes de la llave de cierre):

```csharp
    public async Task<(ServiceStatus, ClienteDto?, string)> ConsultarDocumento(string tipoDoc, string numero)
    {
        try
        {
            var token = configuration.GetValue<string>("ConsultaDocumento:Token");

            if (!string.IsNullOrEmpty(token))
            {
                // ponytail: no hay proveedor de RENIEC/SUNAT elegido todavia (apis.net.pe,
                // Decolecta, Factiliza, etc.) -- cuando se elija uno, reemplazar este bloque
                // por el HTTP call real usando `token`. Mientras tanto cae al fallback de BD.
            }

            var cliente = await dbContext.Cliente.AsNoTracking()
                .Include(i => i.Ubigeo)
                .FirstOrDefaultAsync(c => c.NumeroDocumento == numero);

            if (cliente == null)
                return (ServiceStatus.Ok, null, "No se encontró el cliente");

            var dto = mapper.Map<ClienteDto>(cliente);

            return (ServiceStatus.Ok, dto, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar documento -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
```

En `Backend/Application/Services/ClienteService.cs`, agregar (siguiendo el mismo patrón que los demás métodos de ese archivo, que ya se leyó parcialmente vía `ClientesController`/`IClienteService` — el patrón exacto de `ErrorHandler` es el mismo usado en `ExtensionesService`/`ComprobanteService` ya vistos: `estado != ServiceStatus.Ok` lanza `ErrorHandler`):

```csharp
        public async Task<MessageResult<object>> ConsultarDocumento(string tipoDoc, string numero)
        {
            var (estado, resp, message) = await clienteRepository.ConsultarDocumento(tipoDoc, numero);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(HttpStatusCode.InternalServerError, message, resp);

            return MessageResult<object>.Of(message, resp);
        }
```

(el campo privado en `ClienteService.cs` se llama `clienteRepository`, sin guion bajo — distinto a la convención `_extensionesRepository` usada en otros servicios; agregar este método dentro de la clase `ClienteService`, por ejemplo después de `GetClientes`.)

En `Backend/WEB_API/Controllers/ClientesController.cs`, agregar:

```csharp
    [HttpGet("consultar-documento")]
    public async Task<IActionResult> ConsultarDocumento([FromQuery] string tipoDoc, [FromQuery] string numero)

        => Ok(await _clienteService.ConsultarDocumento(tipoDoc, numero));
```

- [ ] **Step 4: Correr los tests para confirmar que pasan**

Run: `cd Backend && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ClienteRepositoryTests"`
Expected: los 2 `PASS`.

- [ ] **Step 5: Compilar y correr toda la suite**

Run: `cd Backend && dotnet build WEB_API_SPA.sln && dotnet test Tests/Infrastructure.Tests/Infrastructure.Tests.csproj`
Expected: `0 Errores`, todos los tests en verde.

- [ ] **Step 6: Commit**

```bash
git add Backend/Application/Interfaces/IRepository/IClienteRepository.cs \
        Backend/Application/Interfaces/IServices/IClienteService.cs \
        Backend/Infrastructure/Repositories/ClienteRepository.cs \
        Backend/Application/Services/ClienteService.cs \
        Backend/WEB_API/Controllers/ClientesController.cs \
        Backend/Tests/Infrastructure.Tests/ClienteRepositoryTests.cs
git commit -m "feat: add GET /clientes/consultar-documento (DB lookup now, external API extension point later)"
```

---

### Task 8: Frontend — tipos TS y llamadas API nuevas

**Files:**
- Modify: `Frontend/src/redux/reducers/ventas/interfaces/index.ts`

**Interfaces:**
- Produces: `ISaleProduct.{direccion, ubigeoId, celular, enviarComprobanteEmail, esCredito, descuentoPorcentaje, totalRecibido, observacion, modoEnvio}`, `IDetalleComprobante.{tipoIgv, unidadMedida}` — consumidos por Task 9/10/11.

- [ ] **Step 1: Extender las interfaces**

En `Frontend/src/redux/reducers/ventas/interfaces/index.ts`, reemplazar:

```typescript
export interface ISaleProduct {
    clientId?: any
    tipoDocumentoVentaId: number
    numeroDocumento?: string
    fechaVenta?: string
    total: number
    ruc: string
    razonSocial: string
    efectivo: string
    tipoVenta?: string
    esEcommerce?: boolean
    tipoEnvio?: string
    distrito?: string
    detalleComprobante: IDetalleComprobante[] 
    detallePago: IDetallePago[]
}

export interface IDetalleComprobante {
    productoId: number
    cantidad: number
    valorUnitario: number
    costoReal?: number
}
```

por:

```typescript
export interface ISaleProduct {
    clientId?: any
    tipoDocumentoVentaId: number
    numeroDocumento?: string
    fechaVenta?: string
    total: number
    ruc: string
    razonSocial: string
    efectivo: string
    tipoVenta?: string
    esEcommerce?: boolean
    tipoEnvio?: string
    distrito?: string
    direccion?: string
    ubigeoId?: string
    celular?: string
    enviarComprobanteEmail?: boolean
    esCredito?: boolean
    descuentoPorcentaje?: number
    totalRecibido?: number
    observacion?: string
    modoEnvio?: "F" | "S" | "G"
    detalleComprobante: IDetalleComprobante[] 
    detallePago: IDetallePago[]
}

export interface IDetalleComprobante {
    productoId: number
    cantidad: number
    valorUnitario: number
    costoReal?: number
    tipoIgv?: string
    unidadMedida?: string
}
```

(`IDetallePago` no cambia — ya tiene `metodoPagoId`, `monto`, `referenciaOperacion`.)

- [ ] **Step 2: Typecheck**

Run: `cd Frontend && npx tsc --noEmit`
Expected: exit code 0 (los cambios son campos opcionales agregados, no rompen usos existentes).

- [ ] **Step 3: Commit**

```bash
git add Frontend/src/redux/reducers/ventas/interfaces/index.ts
git commit -m "feat: add discount/credit/document-lookup fields to ISaleProduct/IDetalleComprobante"
```

---

### Task 9: Frontend — sección Cliente del formulario (con lupa)

**Files:**
- Modify: `Frontend/src/presentation/views/Modules/NuevaFactura/index.tsx`
- Modify: `Frontend/src/presentation/views/Modules/NuevaFactura/nuevaFactura.module.css` (si no existe con ese nombre exacto, usar el archivo `.module.css` que ya importa `index.tsx` como `styles` — confirmar el nombre real del archivo con `ls Frontend/src/presentation/views/Modules/NuevaFactura/` antes de editarlo, ya que no se leyó su contenido durante la planificación, solo se leyó `index.tsx`)

Este task reemplaza el estado y la sección "Cliente" de `NuevaFactura`; las secciones de productos/pagos/resumen/envío se rehacen en las Tasks 10 y 11 sobre el mismo archivo ya modificado aquí.

**Nota de secuencia:** las Tasks 9, 10 y 11 son una sola reescritura de `index.tsx` partida en 3 diffs revisables — el archivo **no compila entre tareas** (Task 9 borra `nombre`/`apellido`/`dni`/`ruc`/`tipoDocumento`, pero `generarFactura` y `limpiar` — que todavía los referencian — recién se reescriben en la Task 11). Por eso el `Step 4` de las Tasks 9 y 10 pide smoke visual en vez de `npx tsc --noEmit` (que fallaría a propósito hasta terminar la Task 11). El primer punto de verificación real con `tsc` y con el navegador de punta a punta es el `Step 6` de la Task 11. Ejecutar las tres tareas seguidas, sin parar a "usar" la pantalla entre medio.

- [ ] **Step 1: Reemplazar el estado del componente**

En `Frontend/src/presentation/views/Modules/NuevaFactura/index.tsx`, reemplazar el bloque de `useState` (líneas 41-54 del archivo actual, desde `const [nombre, setNombre] = useState("");` hasta `const [clientesEncontrados, setClientesEncontrados] = useState<any[]>([]);`) por:

```tsx
  const [tipoDocIdentificacion, setTipoDocIdentificacion] = useState<"boleta" | "factura" | "nota-venta">("boleta");
  const [numeroDocumento, setNumeroDocumento] = useState("");
  const [razonSocial, setRazonSocial] = useState("");
  const [direccion, setDireccion] = useState("");
  const [ubigeoId, setUbigeoId] = useState("");
  const [celular, setCelular] = useState("");
  const [enviarComprobanteEmail, setEnviarComprobanteEmail] = useState(false);
  const [buscandoDocumento, setBuscandoDocumento] = useState(false);
  const [ubigeos, setUbigeos] = useState<any[]>([]);
  const [fechaVenta, setFechaVenta] = useState<string>(new Date().toISOString().slice(0, 10));
  const [isPickerOpen, setIsPickerOpen] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [isOpenLoadingPay, setIsOpenLoadingPay] = useState(false);
```

(`tipoDocumento` pasa a llamarse `tipoDocIdentificacion` para que coincida con el label del mockup — el resto del archivo que use `tipoDocumento` en este task queda referenciando el nombre viejo hasta que las Tasks 10/11 terminen de reemplazar el resto del cuerpo; renombrar todas las referencias restantes a `tipoDocumento` dentro de este mismo archivo por `tipoDocIdentificacion` como parte de este mismo Step, ya que TypeScript no compila con el nombre viejo indefinido.)

`metodoPagoId` se mantiene (se usa en Task 11); revisar que sigue declarado más abajo en el archivo original y no se borró por error al reemplazar el bloque.

- [ ] **Step 2: Cargar ubigeos y reemplazar la lógica de búsqueda de cliente**

En el `useEffect` que hace `dispatch(getProducts(...)); dispatch(getPayMethods());`, agregar la carga de ubigeos:

```tsx
  useEffect(() => {
    if (!getToken()) {
      navigate("/");
      return;
    }
    dispatch(resetSale());
    dispatch(getProducts(0, 0, "", 1, 100, undefined));
    dispatch(getPayMethods());
    axiosInstance.get("/extensiones/ubigeos").then(({ data }: any) => setUbigeos(data?.data || []));
  }, []);
```

(`axiosInstance` ya está importado en el archivo actual — confirmar el import exacto arriba del archivo antes de usarlo tal cual.)

Reemplazar `seleccionarCliente`/`buscarCliente` (líneas 116-142 del archivo actual) por:

```tsx
  const buscarPorDocumento = async () => {
    if (numeroDocumento.trim().length < 8) {
      return toast.error("Ingresa un número de documento válido");
    }
    setBuscandoDocumento(true);
    try {
      const tipoDoc = numeroDocumento.trim().length === 11 ? "ruc" : "dni";
      const { data }: any = await axiosInstance.get("/clientes/consultar-documento", {
        params: { tipoDoc, numero: numeroDocumento.trim() },
      });
      const cliente = data?.data;
      if (cliente) {
        setRazonSocial(cliente.nombre ?? "");
        setDireccion(cliente.direccion ?? "");
        setCelular(cliente.telefono ?? "");
        setUbigeoId(cliente.ubigeoId ?? "");
        toast.success("Cliente encontrado");
      } else {
        toast.error("No se encontró, completa los datos manualmente");
      }
    } catch {
      toast.error("Error al buscar el documento");
    } finally {
      setBuscandoDocumento(false);
    }
  };
```

- [ ] **Step 3: Reemplazar el JSX de la sección Cliente**

Reemplazar el bloque `<div className={styles.section}><h4>Cliente</h4>...</div>` completo (líneas 221-280 del archivo actual, todo el primer `<div className={styles.section}>` de la grilla) por:

```tsx
        <div className={styles.section}>
          <h4>Cliente</h4>

          <div className={styles.formGrid}>
            <div>
              <label>Tipo Doc. Ident.</label>
              <select value={tipoDocIdentificacion} onChange={(e) => setTipoDocIdentificacion(e.target.value as any)}>
                <option value="boleta">DNI</option>
                <option value="factura">R.U.C.</option>
              </select>
            </div>
            <div>
              <label>N° de documento</label>
              <div className={styles.buscarClienteRow}>
                <input
                  value={numeroDocumento}
                  maxLength={tipoDocIdentificacion === "factura" ? 11 : 8}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => setNumeroDocumento(e.target.value.replace(/\D/g, ""))}
                  placeholder="Número de documento aquí"
                />
                <button type="button" className={styles.searchBtn} onClick={buscarPorDocumento} disabled={buscandoDocumento}>
                  <Icon icon="iconamoon:search-bold" />
                </button>
              </div>
            </div>
            <div>
              <label>Razón Social</label>
              <input value={razonSocial} onChange={(e: ChangeEvent<HTMLInputElement>) => setRazonSocial(e.target.value)} placeholder="Nombre o Razón Social aquí" />
            </div>
          </div>

          <div className={styles.formGrid}>
            <div>
              <label>Dirección</label>
              <input value={direccion} onChange={(e: ChangeEvent<HTMLInputElement>) => setDireccion(e.target.value)} placeholder="Escribe aquí la dirección completa" />
            </div>
            <div>
              <label>Ubigeo</label>
              <select value={ubigeoId} onChange={(e) => setUbigeoId(e.target.value)}>
                <option value="">Selecciona tu código de ubigeo</option>
                {ubigeos.map((u: any) => (
                  <option key={u.ubigeoId} value={u.ubigeoId}>
                    {u.departamento} - {u.provincia} - {u.distrito}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label>Num. Celular</label>
              <input value={celular} onChange={(e: ChangeEvent<HTMLInputElement>) => setCelular(e.target.value.replace(/\D/g, ""))} placeholder="Escribe el número de celular" />
            </div>
          </div>

          <div className={styles.toggleRow}>
            <label>
              <input type="checkbox" checked={enviarComprobanteEmail} onChange={(e) => setEnviarComprobanteEmail(e.target.checked)} />
              ¿Deseas enviar el comprobante electrónico al email del cliente?
            </label>
          </div>

          <div>
            <label>Fecha</label>
            <input type="date" value={fechaVenta} onChange={(e: ChangeEvent<HTMLInputElement>) => setFechaVenta(e.target.value)} />
          </div>
        </div>
```

El siguiente `<div className={styles.section}><h4>Tipo de documento</h4>...</div>` (líneas 282-320 del original, con los radios Boleta/Factura/Nota de venta y el campo RUC aparte) se elimina — ya quedó fusionado en la sección Cliente de arriba (el select "Tipo Doc. Ident." + el campo único de documento reemplazan tanto el radio como el campo RUC separado). Borrar ese bloque completo.

- [ ] **Step 4: Levantar el frontend y verificar visualmente la sección Cliente**

Run: `cd Frontend && npm run dev` (dejarlo corriendo en background)
Abrir `http://localhost:5173/dashboard/nueva-factura` (o el puerto que informe Vite) en el navegador, iniciar sesión si hace falta, y confirmar: el select "Tipo Doc. Ident.", el campo de documento con lupa, Razón Social, Dirección, Ubigeo (con opciones cargadas), Celular, y el toggle de email se ven y no tiran errores en la consola del navegador. La lupa debe mostrar "No se encontró..." o autocompletar si el documento existe en la BD de prueba.

- [ ] **Step 5: Commit**

```bash
git add Frontend/src/presentation/views/Modules/NuevaFactura/
git commit -m "feat: rebuild the Cliente section of NuevaFactura per the reference mockup"
```

---

### Task 10: Frontend — tabla de productos con Tipo IGV / Und. Medida y acciones Editar/Agregar/Eliminar

**Files:**
- Modify: `Frontend/src/presentation/views/Modules/NuevaFactura/index.tsx`

- [ ] **Step 1: Agregar estado de selección de fila y defaults de IGV/unidad al agregar productos**

Agregar, junto a los demás `useState` de este archivo:

```tsx
  const [filaSeleccionada, setFilaSeleccionada] = useState<number | null>(null);
```

Reemplazar `agregarProductos` (líneas 91-96 del archivo original):

```tsx
  const agregarProductos = (seleccionados: any[]) => {
    seleccionados.forEach((p) => dispatch(getProductsBySale({ ...p, tipoIgv: "10", unidadMedida: "NIU" }) as any));
    if (seleccionados.length > 0) {
      toast.success(`${seleccionados.length} producto(s) agregado(s)`);
    }
  };
```

- [ ] **Step 2: Agregar el handler para cambiar Tipo IGV / Unidad por línea**

Junto a `cambiarCosto` (líneas 102-114 del archivo original), agregar:

```tsx
  const cambiarTipoIgv = (productoId: number, valor: string) => {
    const actualizados = productsBySale.map((p: any) => (p.productoId === productoId ? { ...p, tipoIgv: valor } : p));
    dispatch(updateProductByPrice(actualizados) as any);
  };

  const cambiarUnidadMedida = (productoId: number, valor: string) => {
    const actualizados = productsBySale.map((p: any) => (p.productoId === productoId ? { ...p, unidadMedida: valor } : p));
    dispatch(updateProductByPrice(actualizados) as any);
  };
```

(`updateProductByPrice` ya existe en el reducer — `Frontend/src/redux/reducers/ventas/ventas.reducer.ts:532`, `dispatch({ type: types.UPDATE_PRODUCT_BY_PRICE, payload: products })` — reemplaza `productsBySale` completo por el array que se le pase, igual patrón que ya usa `cambiarCosto`.)

- [ ] **Step 3: Reemplazar la tabla de productos**

Reemplazar el bloque `<div className={styles.section}>` de productos (líneas 322-382 del archivo original, desde `<div className={styles.productosHeader}>` hasta el cierre de la tabla) por:

```tsx
        <div className={styles.section}>
          <div className={styles.productosHeader}>
            <h4>Lista de productos:</h4>
            <div className={styles.productosAcciones}>
              <button type="button" className={styles.editBtn} disabled={filaSeleccionada === null}>
                <Icon icon="mdi:pencil" /> Editar
              </button>
              <button type="button" className={styles.addBtn} onClick={() => setIsPickerOpen(true)}>
                <Icon icon="mdi:plus" /> Agregar (F1)
              </button>
              <button
                type="button"
                className={styles.removeBtnLg}
                disabled={filaSeleccionada === null}
                onClick={() => {
                  if (filaSeleccionada !== null) eliminar(filaSeleccionada);
                  setFilaSeleccionada(null);
                }}
              >
                <Icon icon="mdi:trash-can-outline" /> Eliminar
              </button>
            </div>
          </div>

          {productsBySale.length === 0 ? (
            <p className={styles.empty}>Aún no agregaste productos.</p>
          ) : (
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>Descripción</th>
                    <th>Tipo IGV</th>
                    <th>Und/Medida</th>
                    <th>Precio</th>
                    <th>Cantidad</th>
                    <th>Sub.Total</th>
                    <th>Igv</th>
                    <th>Importe</th>
                  </tr>
                </thead>
                <tbody>
                  {productsBySale.map((item: any) => {
                    const subtotal = item.precio * item.cantidad;
                    const igvLinea = item.tipoIgv === "10" ? subtotal - subtotal / 1.18 : 0;
                    return (
                      <tr
                        key={item.productoId}
                        className={filaSeleccionada === item.productoId ? styles.filaSeleccionada : undefined}
                        onClick={() => setFilaSeleccionada(item.productoId)}
                      >
                        <td data-label="Descripción">{item.nombre}</td>
                        <td data-label="Tipo IGV">
                          <select value={item.tipoIgv ?? "10"} onChange={(e) => cambiarTipoIgv(item.productoId, e.target.value)} onClick={(e) => e.stopPropagation()}>
                            <option value="10">Gravado - Op. Onerosa</option>
                            <option value="20">Exonerado</option>
                            <option value="30">Inafecto</option>
                          </select>
                        </td>
                        <td data-label="Und/Medida">
                          <select value={item.unidadMedida ?? "NIU"} onChange={(e) => cambiarUnidadMedida(item.productoId, e.target.value)} onClick={(e) => e.stopPropagation()}>
                            <option value="NIU">NIU - Unidad</option>
                            <option value="ZZ">ZZ - Servicio</option>
                            <option value="KGM">KGM - Kilogramo</option>
                          </select>
                        </td>
                        <td data-label="Precio">S/ {Number(item.precio).toFixed(2)}</td>
                        <td data-label="Cantidad">
                          <div className={styles.qtyControl} onClick={(e) => e.stopPropagation()}>
                            <button type="button" onClick={() => restar(item)}>-</button>
                            <span>{item.cantidad}</span>
                            <button type="button" onClick={() => sumar(item)}>+</button>
                          </div>
                        </td>
                        <td data-label="Sub.Total">S/ {subtotal.toFixed(2)}</td>
                        <td data-label="Igv">S/ {igvLinea.toFixed(2)}</td>
                        <td data-label="Importe">S/ {subtotal.toFixed(2)}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
```

(se quita la columna "Costo real" del mockup a propósito — no aparece en la imagen de referencia; `cambiarCosto` deja de usarse en el JSX pero se mantiene la función por si se necesita en otro lado del archivo — si `tsc` marca la función como no usada, borrarla junto con su `useState`/import relacionado.)

- [ ] **Step 4: Typecheck y smoke visual**

Run: `cd Frontend && npx tsc --noEmit`
Expected: exit code 0.

Con el dev server corriendo (Task 9, Step 4), agregar un producto desde "Agregar (F1)", confirmar que aparece en la tabla con los selects de Tipo IGV/Unidad, que cambiarlos no rompe nada visualmente, que +/- cambian cantidad, y que click en una fila la resalta y habilita "Editar"/"Eliminar".

- [ ] **Step 5: Commit**

```bash
git add Frontend/src/presentation/views/Modules/NuevaFactura/
git commit -m "feat: add Tipo IGV/Und Medida per line and row selection to the products table"
```

---

### Task 11: Frontend — multipagos, crédito/descuento/vuelto, resumen, observación, modo de envío y submit

**Files:**
- Modify: `Frontend/src/presentation/views/Modules/NuevaFactura/index.tsx`

- [ ] **Step 1: Agregar el estado restante**

Agregar, junto a los demás `useState`:

```tsx
  const [multipagos, setMultipagos] = useState(false);
  const [pagos, setPagos] = useState<{ metodoPagoId: number; monto: number }[]>([{ metodoPagoId: 0, monto: 0 }]);
  const [esCredito, setEsCredito] = useState(false);
  const [descuentoPorcentaje, setDescuentoPorcentaje] = useState(0);
  const [totalRecibidoInput, setTotalRecibidoInput] = useState("");
  const [observacion, setObservacion] = useState("");
  const [modoEnvio, setModoEnvio] = useState<"F" | "S" | "G">("G");
```

- [ ] **Step 2: Calcular descuento, resumen y vuelto en vivo**

Reemplazar el bloque de cálculo de totales (líneas 84-89 del archivo original: `const total = ...`, `const subtotal = ...`, `const igv = ...`) por:

```tsx
  const gravada = productsBySale.reduce((acc, item: any) => acc + item.precio * item.cantidad, 0);
  const descuentoTotal = Math.round((gravada * descuentoPorcentaje) / 100 * 100) / 100;
  const total = Math.round((gravada - descuentoTotal) * 100) / 100;
  const subtotal = total / 1.18;
  const igv = total - subtotal;
  const totalRecibido = parseFloat(totalRecibidoInput) || 0;
  const vuelto = Math.round((totalRecibido - total) * 100) / 100;
```

- [ ] **Step 3: Reemplazar la sección de método de pago por Multipagos + Crédito + Forma de pago + Descuento/Recibido/Vuelto + Resumen**

Reemplazar el bloque `<div className={styles.section}><h4>Método de pago</h4>...</div>` y el bloque de totales que le sigue (líneas 384-413 del archivo original) por:

```tsx
        <div className={styles.section}>
          <div className={styles.toggleRow}>
            <label>
              <input type="checkbox" checked={multipagos} onChange={(e) => {
                setMultipagos(e.target.checked);
                if (!e.target.checked) setPagos([{ metodoPagoId: pagos[0]?.metodoPagoId ?? 0, monto: total }]);
              }} />
              Multipagos
            </label>
          </div>

          {!multipagos ? (
            <div>
              <label>Forma de Pago</label>
              <select
                className={styles.select}
                value={pagos[0]?.metodoPagoId ?? 0}
                onChange={(e) => setPagos([{ metodoPagoId: Number(e.target.value), monto: total }])}
              >
                <option value={0}>Selecciona un método de pago</option>
                {(payMethods as any[])?.map((m) => (
                  <option key={m.id} value={m.id}>{m.value}</option>
                ))}
              </select>
            </div>
          ) : (
            <div>
              {pagos.map((p, idx) => (
                <div key={idx} className={styles.fieldRow}>
                  <select
                    className={styles.select}
                    value={p.metodoPagoId}
                    onChange={(e) => setPagos(pagos.map((x, i) => (i === idx ? { ...x, metodoPagoId: Number(e.target.value) } : x)))}
                  >
                    <option value={0}>Método de pago</option>
                    {(payMethods as any[])?.map((m) => (
                      <option key={m.id} value={m.id}>{m.value}</option>
                    ))}
                  </select>
                  <input
                    type="number"
                    value={p.monto}
                    onChange={(e) => setPagos(pagos.map((x, i) => (i === idx ? { ...x, monto: parseFloat(e.target.value) || 0 } : x)))}
                  />
                  {pagos.length > 1 && (
                    <button type="button" onClick={() => setPagos(pagos.filter((_, i) => i !== idx))}>
                      <Icon icon="mdi:trash-can-outline" />
                    </button>
                  )}
                </div>
              ))}
              <button type="button" onClick={() => setPagos([...pagos, { metodoPagoId: 0, monto: 0 }])}>
                <Icon icon="mdi:plus" /> Agregar pago
              </button>
            </div>
          )}

          <div className={styles.toggleRow}>
            <label>
              <input type="checkbox" checked={esCredito} onChange={(e) => setEsCredito(e.target.checked)} />
              ¿Es al crédito?
            </label>
          </div>

          <div className={styles.fieldRow}>
            <div>
              <label>Descuento en %</label>
              <input type="number" min={0} max={100} value={descuentoPorcentaje} onChange={(e) => setDescuentoPorcentaje(parseFloat(e.target.value) || 0)} />
            </div>
            {!esCredito && (
              <>
                <div>
                  <label>Total Recibido S/.</label>
                  <input type="number" value={totalRecibidoInput} onChange={(e) => setTotalRecibidoInput(e.target.value)} />
                </div>
                <div>
                  <label>Vuelto S/.</label>
                  <input type="text" value={vuelto.toFixed(2)} disabled />
                </div>
              </>
            )}
          </div>

          <div>
            <label>Observación:</label>
            <textarea value={observacion} onChange={(e) => setObservacion(e.target.value)} placeholder="Escribe aquí una observación" />
          </div>
        </div>

        <div className={styles.section}>
          <div className={styles.totalesRow}>
            <span>Gravada</span>
            <span>S/ {gravada.toFixed(2)}</span>
          </div>
          <div className={styles.totalesRow}>
            <span>(-) Descuento Total</span>
            <span>S/ {descuentoTotal.toFixed(2)}</span>
          </div>
          <div className={styles.totalesRow}>
            <span>IGV (18%)</span>
            <span>S/ {igv.toFixed(2)}</span>
          </div>
          <div className={`${styles.totalesRow} ${styles.totalesRowFinal}`}>
            <span>Total</span>
            <span>S/ {total.toFixed(2)}</span>
          </div>
        </div>

        <div className={styles.section}>
          <h4>Selecciona el modo de envío:</h4>
          <div className={styles.modoEnvioRow}>
            <label>
              <input type="radio" checked={modoEnvio === "F"} onChange={() => setModoEnvio("F")} />
              Solo Firmar e Imprimir
            </label>
            <label>
              <input type="radio" checked={modoEnvio === "S"} onChange={() => setModoEnvio("S")} />
              Enviar a SUNAT ahora mismo!
            </label>
            <label>
              <input type="radio" checked={modoEnvio === "G"} onChange={() => setModoEnvio("G")} />
              Solo Guardar la Venta
            </label>
          </div>
        </div>
```

- [ ] **Step 4: Reescribir `generarFactura` y el botón final**

Reemplazar `generarFactura` (líneas 148-196 del archivo original) por:

```tsx
  const generarFactura = () => {
    if (razonSocial.trim() === "") {
      return toast.error("La Razón Social es obligatoria");
    }
    if (numeroDocumento.trim().length !== (tipoDocIdentificacion === "factura" ? 11 : 8)) {
      return toast.error(tipoDocIdentificacion === "factura" ? "El RUC debe tener 11 dígitos" : "El DNI debe tener 8 dígitos");
    }
    if (productsBySale.length === 0) {
      return toast.error("Agrega al menos un producto");
    }
    if (!multipagos && (pagos[0]?.metodoPagoId ?? 0) === 0) {
      return toast.error("Elige un método de pago");
    }
    if (multipagos) {
      const sumaPagos = Math.round(pagos.reduce((acc, p) => acc + p.monto, 0) * 100) / 100;
      if (sumaPagos !== Math.round(total * 100) / 100) {
        return toast.error("La suma de los pagos no coincide con el total");
      }
    }
    if (!esCredito && vuelto < 0) {
      return toast.error("El monto recibido es menor al total");
    }

    const tipoDocumentoVentaId = tipoDocIdentificacion === "boleta" ? 2 : 1;
    const metodoPagoSeleccionado = (payMethods as any[])?.find((m) => m.id === pagos[0]?.metodoPagoId);

    const payload: ISaleProduct = {
      clientId: null,
      tipoDocumentoVentaId,
      numeroDocumento,
      razonSocial: razonSocial.trim(),
      ruc: tipoDocIdentificacion === "factura" ? numeroDocumento : "",
      efectivo: metodoPagoSeleccionado?.value?.toUpperCase() || "",
      tipoVenta: tipoDocIdentificacion,
      total,
      fechaVenta,
      esEcommerce: false,
      tipoEnvio: "LOCAL",
      distrito: "",
      direccion,
      ubigeoId,
      celular,
      enviarComprobanteEmail,
      esCredito,
      descuentoPorcentaje,
      totalRecibido: esCredito ? undefined : totalRecibido,
      observacion,
      modoEnvio,
      detalleComprobante: productsBySale.map((item: any) => ({
        productoId: item.productoId,
        cantidad: item.cantidad,
        valorUnitario: item.precio,
        costoReal: item.costoReal,
        tipoIgv: item.tipoIgv ?? "10",
        unidadMedida: item.unidadMedida ?? "NIU",
      })),
      detallePago: pagos.map((p) => ({
        metodoPagoId: p.metodoPagoId,
        monto: p.monto,
        referenciaOperacion: "",
      })),
    };

    setEnviando(true);
    setIsOpenLoadingPay(true);
    dispatch(saleProducts(payload) as any);
  };
```

Reemplazar el botón de submit (líneas 415-422 del archivo original, dentro de `<div className={styles.buttons}>`) por:

```tsx
        <div className={styles.buttons}>
          <button type="button" className={styles.submit} disabled={enviando} onClick={generarFactura}>
            {enviando ? "Guardando..." : "GUARDAR DOCUMENTO ELECTRÓNICO (F12)"}
          </button>
        </div>
```

(se quita el botón "Limpiar" del mockup a propósito — no aparece en la imagen de referencia; si el usuario lo pide después, es un `onClick` trivial de agregar reusando `limpiar` ya existente.)

- [ ] **Step 5: Atajo de teclado F12 para guardar**

Agregar un `useEffect` nuevo, junto a los demás:

```tsx
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "F12") {
        e.preventDefault();
        generarFactura();
      }
      if (e.key === "F1") {
        e.preventDefault();
        setIsPickerOpen(true);
      }
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [generarFactura]);
```

(si TypeScript se queja de que `generarFactura` cambia en cada render y el linter pide envolverla en `useCallback`, dejarlo así igual — el resto del archivo ya tiene el mismo patrón de funciones recreadas en cada render sin `useCallback`, seguir la convención existente del archivo en vez de introducir una nueva.)

- [ ] **Step 6: Typecheck y smoke test completo en navegador**

Run: `cd Frontend && npx tsc --noEmit`
Expected: exit code 0.

Con el dev server corriendo, hacer una venta completa de punta a punta en `http://localhost:5173/dashboard/nueva-factura`:
1. Buscar/completar cliente, agregar 1-2 productos.
2. Probar Multipagos apagado (un método de pago) y encendido (dos líneas, validar que la suma tiene que calzar con el total).
3. Probar "Es al Crédito" encendido (Total Recibido/Vuelto deben desaparecer) y apagado (deben validar vuelto negativo).
4. Probar Descuento en % y confirmar que el Resumen (Gravada/Descuento/IGV/Total) se actualiza en vivo.
5. Probar los 3 modos de envío y confirmar (revisando la respuesta de red en devtools o el mensaje de éxito) que la venta se crea sin error 500 en los tres casos.
6. Probar F1 (abre el picker de productos) y F12 (dispara guardar) desde el teclado.

- [ ] **Step 7: Commit**

```bash
git add Frontend/src/presentation/views/Modules/NuevaFactura/
git commit -m "feat: add multipagos, credito, descuento, vuelto, resumen and modo de envio to NuevaFactura"
```

---

### Task 12: CSS — layout final calcado del mockup

**Files:**
- Modify: `Frontend/src/presentation/views/Modules/NuevaFactura/nuevaFactura.module.css` (confirmar nombre exacto del archivo, ver nota en Task 9)

**Interfaces:**
- Consumes: todas las clases `styles.*` referenciadas en Tasks 9/10/11 (`toggleRow`, `productosAcciones`, `editBtn`, `removeBtnLg`, `filaSeleccionada`, `fieldRow`, `modoEnvioRow`, más las que ya existían: `section`, `formGrid`, `buscarClienteRow`, `searchBtn`, `tableWrap`, `table`, `qtyControl`, `totalesRow`, `totalesRowFinal`, `select`, `empty`, `buttons`, `submit`, `addBtn`).

- [ ] **Step 1: Agregar las clases nuevas**

Antes de escribir CSS, leer el archivo `.module.css` completo para ver las clases que ya existen (`section`, `formGrid`, etc.) y no duplicar reglas. Agregar al final del archivo:

```css
.toggleRow {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0.75rem 0;
}

.toggleRow label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  cursor: pointer;
}

.productosAcciones {
  display: flex;
  gap: 0.5rem;
}

.editBtn,
.removeBtnLg {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  border: none;
  cursor: pointer;
  font-size: 0.875rem;
}

.editBtn {
  background: #6366f1;
  color: white;
}

.removeBtnLg {
  background: #ef4444;
  color: white;
}

.editBtn:disabled,
.removeBtnLg:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.filaSeleccionada {
  background: #eef2ff;
}

.fieldRow {
  display: flex;
  gap: 1rem;
  margin: 0.75rem 0;
}

.fieldRow > div {
  flex: 1;
}

.modoEnvioRow {
  display: flex;
  gap: 2rem;
  flex-wrap: wrap;
}

.modoEnvioRow label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  cursor: pointer;
}
```

- [ ] **Step 2: Smoke visual final**

Con el dev server corriendo, recargar `http://localhost:5173/dashboard/nueva-factura` y confirmar que no quedan clases sin estilo (texto pegado, sin espaciado) comparando contra el mockup de referencia. Ajustar valores de `gap`/`padding` a ojo si algo se ve mal — no hace falta pixel-perfect, solo que sea usable y ordenado.

- [ ] **Step 3: Commit**

```bash
git add Frontend/src/presentation/views/Modules/NuevaFactura/
git commit -m "style: finish NuevaFactura layout to match the reference mockup"
```

---

### Task 13: Bump del submódulo Frontend en el repo padre

**Files:**
- Modify: `Frontend` (gitlink, en el repo `D:\Github\PuntoVenta`)

- [ ] **Step 1: Push del submódulo y bump del puntero**

Run (desde `D:\Github\PuntoVenta\Frontend`): `git push` (sube todos los commits de las Tasks 8-12).
Run (desde `D:\Github\PuntoVenta`): `git add Frontend && git commit -m "chore: bump Frontend submodule for nueva emision de factura"`.

Este paso solo aplica si el usuario pidió explícitamente subir los cambios — si no, dejar los commits locales sin `push` y avisar que falta ese paso.
