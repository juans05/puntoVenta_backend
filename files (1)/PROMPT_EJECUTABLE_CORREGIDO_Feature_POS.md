# 🚀 PROMPT EJECUTABLE PARA DEVELOPERS (CORREGIDO)

## ROI de Publicidad Facebook - SPRINT 1 (Semana 1-2)

**⚠️ IMPORTANTE:** Esto es una **FEATURE INTEGRADA EN EL POS EXISTENTE**, no una app separada.

---

## 📌 TL;DR: ¿Qué vamos a construir?

Nueva funcionalidad dentro del menú principal del POS que permite a usuarios:

1. **Descargar Excel de Meta Ads Manager**
2. **Subirlo directamente en el POS** (no en app externa)
3. **Mapear cada anuncio a un producto** (manual + sugerencias fuzzy)
4. **El POS calcula ROI real:** `(Ingresos - Costo Producto - Gasto Ads) / Gasto Ads`
5. **Guardar histórico en la BD existente del POS**

**Flujo:**
```
Usuario abre POS
    ↓
Ve nuevo menú: [Dashboard] [Productos] [Ventas] [ROI PUBLICIDAD] ← NUEVO
    ↓
Hago click en "ROI PUBLICIDAD"
    ↓
Se abre pantalla: "Subir Excel de Meta Ads"
    ↓
Upload → Mapeo automático de productos → Cálculo → Ver resultados
```

---

## 🎯 IMPORTANTE: Esto es INTEGRACIÓN con el POS

### ❌ NO hagamos:
- ❌ App separada / SaaS
- ❌ Tabla nueva `Usuario_ROIPublicidad`
- ❌ API pública
- ❌ Pricing, login separado, etc.

### ✅ SÍ hagamos:
- ✅ Nueva vista en el menú del POS
- ✅ Tabla `GastoPublicidad` (hereda `EntityBase` del POS)
- ✅ Usar BD existente del POS (misma conexión, mismo tenant)
- ✅ Usar autenticación existente del POS
- ✅ Usar lista de productos existente del POS
- ✅ Buscar ventas en `ComprobanteDetalle` existente

**Resultado:** Usuario ve esto en su POS:

```
┌─ PUNTO DE VENTA ──────────────────┐
│ [Dashboard]                       │
│ [Productos]                       │
│ [Ventas]                          │
│ [⭐ ROI PUBLICIDAD]  ← NUEVO BOTÓN
│ [Configuración]                   │
└───────────────────────────────────┘
```

---

## 🏗️ ARQUITECTURA: SHARED DATABASE

```
BACKEND (ASP.NET CORE - EXISTENTE)
  ├─ Controllers/
  │  ├─ GastoPublicidadController ← NUEVA
  │  ├─ ProductoController (existente)
  │  ├─ VentasController (existente)
  │  └─ DashboardController (existente)
  │
  ├─ Domain/Entities/
  │  ├─ GastoPublicidad ← NUEVA (hereda EntityBase)
  │  ├─ Producto (existente)
  │  ├─ ComprobanteCabecera (existente)
  │  └─ ComprobanteDetalle (existente)
  │
  └─ Infrastructure/Repositories/
     ├─ GastoPublicidadRepository ← NUEVA
     └─ (resto existente)

FRONTEND (REACT - EXISTENTE)
  ├─ Modules/
  │  ├─ Admin/
  │  │  ├─ Views/
  │  │  │  ├─ GastoPublicidad/ ← NUEVA CARPETA
  │  │  │  │  ├─ FileUploadPublicidad.tsx
  │  │  │  │  ├─ ResumenRoi.tsx
  │  │  │  │  └─ index.tsx
  │  │  │  ├─ Productos/
  │  │  │  └─ Dashboard/
  │  │
  │  └─ MenuPrincipal.tsx ← AGREGAR LINK A ROI PUBLICIDAD

DATABASE (SQL SERVER - EXISTENTE)
  ├─ GastoPublicidad ← NUEVA TABLA
  ├─ Producto (existente)
  ├─ Comprobante* (existente)
  ├─ Usuario (existente)
  └─ Tenant (existente, MULTI-TENANT)
```

---

## 🎯 SPRINT 1: MVP Core (Sin frills)

### Backend - TAREAS INMEDIATAS

#### Tarea 1: Crear Modelo `GastoPublicidad`

**Archivo a crear:** `Domain/Entities/GastoPublicidad.cs`

```csharp
using System;
using Common.Infrastructure.Repository;

namespace Domain.Entities
{
    /// <summary>
    /// Registra gastos de publicidad (Meta Ads) por anuncio y producto.
    /// IMPORTANTE: Hereda de EntityBase que proporciona TenantId, FechaCreacion, etc.
    /// Esto significa que cada registro está vinculado automáticamente al tenant
    /// del usuario que lo crea.
    /// </summary>
    public class GastoPublicidad : EntityBase
    {
        /// <summary>
        /// FK: Producto que fue anunciado (desde el catálogo existente del POS)
        /// </summary>
        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;

        /// <summary>
        /// Nombre del anuncio tal como aparece en Meta Ads Manager
        /// Ej: "Casacas Impermeables - Men"
        /// </summary>
        public string NombreAnuncio { get; set; } = null!;

        /// <summary>
        /// Nombre del "conjunto de anuncios" (ad set) en Meta
        /// Opcional: puede ser null
        /// </summary>
        public string? NombreConjuntoAnuncios { get; set; }

        /// <summary>
        /// Fecha inicio del período del anuncio (de "Inicio del informe" en Meta)
        /// </summary>
        public DateTime FechaInicio { get; set; }

        /// <summary>
        /// Fecha fin del período del anuncio (de "Fin del informe" en Meta)
        /// </summary>
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Importe total gastado en este anuncio durante el período, en PEN
        /// </summary>
        public decimal ImporteGastado { get; set; }

        /// <summary>
        /// Impresiones del anuncio (opcional, solo referencia)
        /// </summary>
        public int? Impresiones { get; set; }

        /// <summary>
        /// Alcance único del anuncio (opcional, solo referencia)
        /// </summary>
        public int? Alcance { get; set; }

        /// <summary>
        /// Conversiones registradas por Meta (opcional, solo referencia)
        /// </summary>
        public int? Resultados { get; set; }

        /// <summary>
        /// Costo por resultado según Meta (opcional, solo referencia)
        /// </summary>
        public decimal? CostoPorResultado { get; set; }

        /// <summary>
        /// GUID que agrupa todos los registros importados de un mismo Excel
        /// Permite rastrear qué se importó cuándo
        /// </summary>
        public Guid LoteImportacionId { get; set; }
    }
}
```

---

#### Tarea 2: Crear Migración EF Core

**Ubicación:** `Infrastructure/Migrations/`

Ejecutar en Package Manager Console:
```bash
dotnet ef migrations add AddGastoPublicidad --project Infrastructure --startup-project WEB_API
dotnet ef database update
```

**Lo que la migración debe crear:**

```sql
-- TABLA PRINCIPAL
CREATE TABLE [dbo].[GastoPublicidad] (
    [Id] INT NOT NULL IDENTITY (1, 1),
    [ProductoId] INT NOT NULL,
    [NombreAnuncio] NVARCHAR(500) NOT NULL,
    [NombreConjuntoAnuncios] NVARCHAR(500) NULL,
    [FechaInicio] DATETIME2 NOT NULL,
    [FechaFin] DATETIME2 NOT NULL,
    [ImporteGastado] DECIMAL(10, 2) NOT NULL,
    [Impresiones] INT NULL,
    [Alcance] INT NULL,
    [Resultados] INT NULL,
    [CostoPorResultado] DECIMAL(10, 2) NULL,
    [LoteImportacionId] UNIQUEIDENTIFIER NOT NULL,
    
    -- Heredados de EntityBase
    [TenantId] INT NOT NULL,
    [FechaCreacion] DATETIME2 NOT NULL,
    [UsuarioCreacion] NVARCHAR(256) NOT NULL,
    [Estado] INT NOT NULL DEFAULT 1,

    PRIMARY KEY ([Id]),
    FOREIGN KEY ([ProductoId]) REFERENCES [Producto]([Id]),
    FOREIGN KEY ([TenantId]) REFERENCES [Tenant]([Id])
);

-- ÍNDICES PARA PERFORMANCE
CREATE INDEX [IX_GastoPublicidad_ProductoId_Fecha] 
    ON [dbo].[GastoPublicidad] ([ProductoId], [FechaInicio], [FechaFin]);

CREATE INDEX [IX_GastoPublicidad_LoteImportacionId] 
    ON [dbo].[GastoPublicidad] ([LoteImportacionId]);

CREATE INDEX [IX_GastoPublicidad_Tenant_Fecha] 
    ON [dbo].[GastoPublicidad] ([TenantId], [FechaInicio]);
```

---

#### Tarea 3: Repository Interface

**Archivo:** `Application/Interfaces/IGastoPublicidadRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IGastoPublicidadRepository
    {
        /// <summary>
        /// Importa un lote de gastos publicitarios.
        /// Se ejecuta EN TRANSACCIÓN: todo o nada.
        /// </summary>
        Task<(bool success, List<string> errores)> ImportarLoteAsync(
            List<GastoPublicidad> filas,
            Guid loteImportacionId);

        /// <summary>
        /// Calcula ROI real para un producto en un rango de fechas.
        /// 
        /// IMPORTANTE: 
        /// - Ingresos se toman de ComprobantesDetalle.ValorUnitarioTotal
        ///   (lo que el usuario REALMENTE vendió)
        /// - CostoProducto usa Producto.CostoUnitario * Cantidad
        ///   (costo actual del producto, no histórico)
        /// - GastoAds es la suma de ImporteGastado de este repositorio
        /// - ROI = (Ingresos - CostoProducto - GastoAds) / GastoAds
        /// </summary>
        Task<RoiAnalysisDto> CalcularRoiAsync(
            DateTime desde,
            DateTime hasta,
            int? productoId = null);

        /// <summary>
        /// Listado paginado de importaciones históricas.
        /// </summary>
        Task<PaginatedResult<GastoPublicidadDto>> ListarAsync(
            int page,
            int pageSize,
            int? productoId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            Guid? loteImportacionId = null);
    }
}
```

---

#### Tarea 4: Repository Implementation

**Archivo:** `Infrastructure/Repositories/GastoPublicidadRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Data;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class GastoPublicidadRepository : IGastoPublicidadRepository
    {
        private readonly DataContext _context;
        private readonly ILogger<GastoPublicidadRepository> _logger;
        private readonly int _tenantId;

        public GastoPublicidadRepository(
            DataContext context,
            ILogger<GastoPublicidadRepository> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _tenantId = currentUserService.GetTenantId();
        }

        public async Task<(bool success, List<string> errores)> ImportarLoteAsync(
            List<GastoPublicidad> filas,
            Guid loteImportacionId)
        {
            var errores = new List<string>();

            // PASO 1: VALIDAR cada fila
            for (int i = 0; i < filas.Count; i++)
            {
                var fila = filas[i];

                // ProductoId existe y pertenece al tenant
                var productoExiste = await _context.Productos
                    .AnyAsync(p => p.Id == fila.ProductoId && p.TenantId == _tenantId);
                if (!productoExiste)
                    errores.Add($"Fila {i}: ProductoId {fila.ProductoId} no existe o no pertenece a tu empresa");

                // Fechas coherentes
                if (fila.FechaFin < fila.FechaInicio)
                    errores.Add($"Fila {i}: Fecha fin debe ser mayor a fecha inicio");

                // Importe válido
                if (fila.ImporteGastado < 0)
                    errores.Add($"Fila {i}: Importe no puede ser negativo");

                // Nombre anuncio no vacío
                if (string.IsNullOrWhiteSpace(fila.NombreAnuncio))
                    errores.Add($"Fila {i}: Nombre del anuncio no puede estar vacío");
            }

            // Si hay errores, RETORNAR SIN INSERTAR NADA
            if (errores.Any())
            {
                _logger.LogWarning($"Importación rechazada para lote {loteImportacionId}: {errores.Count} errores de validación");
                return (false, errores);
            }

            // PASO 2: INSERTAR en transacción
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Preparar filas: asignar loteId, tenantId, user
                    var usuarioActual = "System"; // TODO: obtener del ICurrentUserService
                    foreach (var fila in filas)
                    {
                        fila.LoteImportacionId = loteImportacionId;
                        fila.TenantId = _tenantId;
                        fila.FechaCreacion = DateTime.UtcNow;
                        fila.UsuarioCreacion = usuarioActual;
                        fila.Estado = 1; // Activo
                    }

                    await _context.GastoPublicidad.AddRangeAsync(filas);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Lote {loteImportacionId} importado exitosamente: {filas.Count} registros");
                    return (true, new List<string>());
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error al insertar lote {loteImportacionId}");
                    errores.Add("Error al guardar en base de datos: " + ex.Message);
                    return (false, errores);
                }
            }
        }

        public async Task<RoiAnalysisDto> CalcularRoiAsync(
            DateTime desde,
            DateTime hasta,
            int? productoId = null)
        {
            // PASO 1: Obtener gastos en publicidad
            var gastosQuery = _context.GastoPublicidad
                .Where(g => g.TenantId == _tenantId)
                .Where(g => g.FechaInicio >= desde && g.FechaFin <= hasta);

            if (productoId.HasValue)
                gastosQuery = gastosQuery.Where(g => g.ProductoId == productoId);

            var gastos = await gastosQuery
                .Include(g => g.Producto)
                .GroupBy(g => g.ProductoId)
                .ToListAsync();

            var resultados = new List<RoiPorProductoDto>();

            // PASO 2: Para cada producto con gasto, calcular ROI
            foreach (var grupo in gastos)
            {
                var prodId = grupo.Key;
                var gastoTotalAds = grupo.Sum(g => g.ImporteGastado);

                // INGRESOS: Buscar comprobantes del usuario en el período de cada anuncio
                var ingresos = await _context.ComprobanteDetalles
                    .Where(d => d.ProductoId == prodId)
                    .Where(d => d.ComprobanteCabecera.TenantId == _tenantId)
                    .Where(d => d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                    // Dentro del rango de ALGUNO de los anuncios del producto
                    .Where(d => grupo.Any(g => 
                        d.ComprobanteCabecera.FechaCreacion >= g.FechaInicio 
                        && d.ComprobanteCabecera.FechaCreacion <= g.FechaFin))
                    .SumAsync(d => (decimal?)d.ValorUnitarioTotal) ?? 0;

                // COSTO: Cantidad * costo unitario actual (no histórico)
                var costoProducto = await _context.ComprobanteDetalles
                    .Where(d => d.ProductoId == prodId)
                    .Where(d => d.ComprobanteCabecera.TenantId == _tenantId)
                    .Where(d => d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                    .Where(d => grupo.Any(g => 
                        d.ComprobanteCabecera.FechaCreacion >= g.FechaInicio 
                        && d.ComprobanteCabecera.FechaCreacion <= g.FechaFin))
                    .SumAsync(d => (decimal?)(d.Cantidad * (d.Producto.CostoUnitario ?? 0))) ?? 0;

                var cantidadVendida = await _context.ComprobanteDetalles
                    .Where(d => d.ProductoId == prodId)
                    .Where(d => d.ComprobanteCabecera.TenantId == _tenantId)
                    .Where(d => d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                    .Where(d => grupo.Any(g => 
                        d.ComprobanteCabecera.FechaCreacion >= g.FechaInicio 
                        && d.ComprobanteCabecera.FechaCreacion <= g.FechaFin))
                    .SumAsync(d => (int?)d.Cantidad) ?? 0;

                // CÁLCULO FINAL
                var utilidadNeta = ingresos - costoProducto - gastoTotalAds;
                var roiPorcentaje = gastoTotalAds > 0 
                    ? utilidadNeta / gastoTotalAds 
                    : (decimal?)null;

                resultados.Add(new RoiPorProductoDto
                {
                    ProductoId = prodId,
                    NombreProducto = grupo.First().Producto.Nombre,
                    GastoAds = gastoTotalAds,
                    Ingresos = ingresos,
                    CostoProducto = costoProducto,
                    UtilidadNeta = utilidadNeta,
                    RoiPorcentaje = roiPorcentaje,
                    CantidadVendida = cantidadVendida,
                    Estado = DeterminarEstado(roiPorcentaje)
                });
            }

            // PASO 3: Resumen agregado
            var resumen = new ResumenRoiDto
            {
                GastoTotalAds = resultados.Sum(r => r.GastoAds),
                IngresosTotal = resultados.Sum(r => r.Ingresos),
                CostoProductoTotal = resultados.Sum(r => r.CostoProducto),
                UtilidadNetaTotal = resultados.Sum(r => r.UtilidadNeta),
                RoiPorcentajePromedio = resultados.Any(r => r.RoiPorcentaje.HasValue) 
                    ? resultados.Average(r => r.RoiPorcentaje ?? 0)
                    : 0
            };

            return new RoiAnalysisDto
            {
                DatosResumen = resumen,
                PorProducto = resultados.OrderByDescending(r => r.RoiPorcentaje).ToList()
            };
        }

        public async Task<PaginatedResult<GastoPublicidadDto>> ListarAsync(
            int page,
            int pageSize,
            int? productoId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            Guid? loteImportacionId = null)
        {
            var query = _context.GastoPublicidad
                .Where(g => g.TenantId == _tenantId)
                .Include(g => g.Producto);

            if (productoId.HasValue)
                query = query.Where(g => g.ProductoId == productoId);
            if (desde.HasValue)
                query = query.Where(g => g.FechaInicio >= desde);
            if (hasta.HasValue)
                query = query.Where(g => g.FechaFin <= hasta);
            if (loteImportacionId.HasValue)
                query = query.Where(g => g.LoteImportacionId == loteImportacionId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(g => g.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new GastoPublicidadDto
                {
                    Id = g.Id,
                    ProductoId = g.ProductoId,
                    NombreProducto = g.Producto.Nombre,
                    NombreAnuncio = g.NombreAnuncio,
                    FechaInicio = g.FechaInicio,
                    FechaFin = g.FechaFin,
                    ImporteGastado = g.ImporteGastado,
                    LoteImportacionId = g.LoteImportacionId
                })
                .ToListAsync();

            return new PaginatedResult<GastoPublicidadDto>(items, total, page, pageSize);
        }

        private string DeterminarEstado(decimal? roi)
        {
            if (roi == null) return "sin_datos";
            if (roi >= 1.0m) return "rentable";      // ROI >= 100%
            if (roi >= 0) return "margen_positivo";  // 0% <= ROI < 100%
            return "perdida";                        // ROI < 0%
        }
    }
}
```

---

#### Tarea 5: DTOs

**Archivo:** `Application/Dtos/GastoPublicidadDto.cs`

```csharp
using System;
using System.Collections.Generic;

namespace Application.Dtos
{
    public class GastoPublicidadDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public string NombreAnuncio { get; set; }
        public string? NombreConjuntoAnuncios { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal ImporteGastado { get; set; }
        public Guid LoteImportacionId { get; set; }
    }

    public class RoiPorProductoDto
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public decimal GastoAds { get; set; }
        public decimal Ingresos { get; set; }
        public decimal CostoProducto { get; set; }
        public decimal UtilidadNeta { get; set; }
        public decimal? RoiPorcentaje { get; set; }
        public int CantidadVendida { get; set; }
        public string Estado { get; set; }
    }

    public class ResumenRoiDto
    {
        public decimal GastoTotalAds { get; set; }
        public decimal IngresosTotal { get; set; }
        public decimal CostoProductoTotal { get; set; }
        public decimal UtilidadNetaTotal { get; set; }
        public decimal RoiPorcentajePromedio { get; set; }
    }

    public class RoiAnalysisDto
    {
        public ResumenRoiDto DatosResumen { get; set; }
        public List<RoiPorProductoDto> PorProducto { get; set; }
    }
}
```

---

#### Tarea 6: Controller

**Archivo:** `WEB_API/Controllers/GastoPublicidadController.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Interfaces;
using Application.Dtos;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WEB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Requiere autenticación existente del POS
    public class GastoPublicidadController : ControllerBase
    {
        private readonly IGastoPublicidadRepository _repo;
        private readonly ILogger<GastoPublicidadController> _logger;

        public GastoPublicidadController(
            IGastoPublicidadRepository repo,
            ILogger<GastoPublicidadController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// Importa un lote de gastos de publicidad desde Excel de Meta Ads.
        /// El Excel ya fue parseado en el navegador, aquí recibimos JSON.
        /// </summary>
        [HttpPost("importar")]
        public async Task<IActionResult> Importar([FromBody] ImportarGastoPublicidadRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var filas = request.Filas.Select(f => new GastoPublicidad
                {
                    ProductoId = f.ProductoId,
                    NombreAnuncio = f.NombreAnuncio,
                    NombreConjuntoAnuncios = f.NombreConjuntoAnuncios,
                    FechaInicio = f.FechaInicio,
                    FechaFin = f.FechaFin,
                    ImporteGastado = f.ImporteGastado,
                    Impresiones = f.Impresiones,
                    Alcance = f.Alcance,
                    Resultados = f.Resultados,
                    CostoPorResultado = f.CostoPorResultado
                }).ToList();

                var (success, errores) = await _repo.ImportarLoteAsync(filas, request.LoteImportacionId);

                if (!success)
                    return BadRequest(new 
                    { 
                        error = "Validación fallida",
                        detalles = errores 
                    });

                return Ok(new
                {
                    success = true,
                    loteImportacionId = request.LoteImportacionId,
                    filasInsertadas = filas.Count,
                    mensaje = $"{filas.Count} registros importados exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando lote");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Calcula ROI de publicidad para un rango de fechas.
        /// GET /api/gastopublicidad/roi?desde=2026-08-01&hasta=2026-08-31&productoId=12
        /// </summary>
        [HttpGet("roi")]
        public async Task<IActionResult> ObtenerRoi(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? productoId = null)
        {
            if (desde > hasta)
                return BadRequest(new { error = "Desde debe ser anterior a hasta" });

            try
            {
                var resultado = await _repo.CalcularRoiAsync(desde, hasta, productoId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando ROI");
                return StatusCode(500, new { error = "Error al calcular ROI" });
            }
        }

        /// <summary>
        /// Lista importaciones históricas (paginado).
        /// GET /api/gastopublicidad?page=1&pageSize=10&productoId=12
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productoId = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "page y pageSize deben ser válidos" });

            try
            {
                var resultado = await _repo.ListarAsync(page, pageSize, productoId, desde, hasta);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando gastos");
                return StatusCode(500, new { error = "Error al listar gastos" });
            }
        }
    }

    public class ImportarGastoPublicidadRequest
    {
        public Guid LoteImportacionId { get; set; }
        public List<GastoPublicidadInputDto> Filas { get; set; }
    }

    public class GastoPublicidadInputDto
    {
        public int ProductoId { get; set; }
        public string NombreAnuncio { get; set; }
        public string? NombreConjuntoAnuncios { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal ImporteGastado { get; set; }
        public int? Impresiones { get; set; }
        public int? Alcance { get; set; }
        public int? Resultados { get; set; }
        public decimal? CostoPorResultado { get; set; }
    }
}
```

---

#### Tarea 6b: Dependency Injection

**Archivo:** `WEB_API/Startup.cs` o `Program.cs` (según tu estructura)

Agregar en `ConfigureServices`:

```csharp
// Agregar estos servicios
services.AddScoped<IGastoPublicidadRepository, GastoPublicidadRepository>();
```

---

### Frontend - TAREAS INMEDIATAS

#### Tarea 7: Componente FileUpload

**Archivo:** `src/presentation/views/Modules/Admin/Views/GastoPublicidad/FileUploadPublicidad.tsx`

```typescript
import React, { useState } from 'react';
import * as XLSX from 'xlsx';
import { axiosInstance } from '@utils/axios';
import { toast } from 'react-toastify';
import { useSelector } from 'react-redux';

interface FilaPreview {
  nombreAnuncio: string;
  nombreConjuntoAnuncios?: string;
  fechaInicio: Date;
  fechaFin: Date;
  importeGastado: number;
  impresiones?: number;
  alcance?: number;
  resultados?: number;
  costoPorResultado?: number;
  productoId: number | null;
  error?: string;
}

export const FileUploadPublicidad = () => {
  const [filas, setFilas] = useState<FilaPreview[]>([]);
  const [loading, setLoading] = useState(false);
  const productos = useSelector(state => state.productos.lista); // Redux selector

  const COLUMNAS_REQUERIDAS = [
    'Inicio del informe',
    'Fin del informe',
    'Nombre del anuncio',
    'Importe gastado (PEN)'
  ];

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      const arrayBuffer = await file.arrayBuffer();
      const workbook = XLSX.read(arrayBuffer, { type: 'array' });
      const sheet = workbook.Sheets[workbook.SheetNames[0]];
      const data = XLSX.utils.sheet_to_json(sheet);

      if (data.length === 0) {
        toast.error('Excel está vacío');
        return;
      }

      const primeraFila = data[0];
      const columnasAusentes = COLUMNAS_REQUERIDAS.filter(
        col => !(col in primeraFila)
      );

      if (columnasAusentes.length > 0) {
        toast.error(`Faltan columnas: ${columnasAusentes.join(', ')}`);
        return;
      }

      const filasParseadas: FilaPreview[] = data.map((row) => {
        const fila: FilaPreview = {
          nombreAnuncio: row['Nombre del anuncio']?.toString().trim() || '',
          nombreConjuntoAnuncios: row['Nombre del conjunto de anuncios']?.toString().trim(),
          fechaInicio: new Date(row['Inicio del informe']),
          fechaFin: new Date(row['Fin del informe']),
          importeGastado: parseFloat(row['Importe gastado (PEN)']) || 0,
          impresiones: row['Impresiones'] ? parseInt(row['Impresiones']) : undefined,
          alcance: row['Alcance'] ? parseInt(row['Alcance']) : undefined,
          resultados: row['Compras'] ? parseInt(row['Compras']) : undefined,
          costoPorResultado: row['Costo por compra (PEN)']
            ? parseFloat(row['Costo por compra (PEN)'])
            : undefined,
          productoId: null
        };

        if (!fila.nombreAnuncio) {
          fila.error = 'Nombre del anuncio vacío';
        }
        if (isNaN(fila.importeGastado) || fila.importeGastado < 0) {
          fila.error = 'Importe gastado inválido';
        }
        if (fila.fechaFin < fila.fechaInicio) {
          fila.error = 'Fecha fin anterior a fecha inicio';
        }

        return fila;
      });

      const filasValidas = filasParseadas.filter(f => !f.error);
      setFilas(filasValidas);

      if (filasParseadas.some(f => f.error)) {
        toast.warning(
          `${filasParseadas.filter(f => f.error).length} filas descartadas`
        );
      }
    } catch (error) {
      console.error(error);
      toast.error('Error al leer Excel');
    }
  };

  const handleProductoChange = (idx: number, productoId: number) => {
    const nuevasFilas = [...filas];
    nuevasFilas[idx].productoId = productoId;
    setFilas(nuevasFilas);
  };

  const handleConfirmar = async () => {
    const sinProducto = filas.filter(f => !f.productoId);
    if (sinProducto.length > 0) {
      toast.error(`${sinProducto.length} fila(s) sin producto asignado`);
      return;
    }

    setLoading(true);
    try {
      const payload = {
        loteImportacionId: crypto.randomUUID(),
        filas: filas.map(f => ({
          productoId: f.productoId,
          nombreAnuncio: f.nombreAnuncio,
          nombreConjuntoAnuncios: f.nombreConjuntoAnuncios,
          fechaInicio: f.fechaInicio.toISOString(),
          fechaFin: f.fechaFin.toISOString(),
          importeGastado: f.importeGastado,
          impresiones: f.impresiones,
          alcance: f.alcance,
          resultados: f.resultados,
          costoPorResultado: f.costoPorResultado
        }))
      };

      const response = await axiosInstance.post(
        '/api/gastopublicidad/importar',
        payload
      );

      if (response.data.success) {
        toast.success(`${response.data.filasInsertadas} registros importados`);
        setFilas([]);
        // TODO: Emitir evento para recargar dashboard ROI
      }
    } catch (error: any) {
      const mensaje = error.response?.data?.error || 'Error al importar';
      toast.error(mensaje);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6 bg-white rounded-lg shadow">
      <h2 className="text-2xl font-bold mb-6">📊 ROI PUBLICIDAD META ADS</h2>

      <div className="mb-6">
        <label className="block text-sm font-medium mb-2">
          Selecciona archivo Excel de Meta Ads Manager
        </label>
        <input
          type="file"
          accept=".xlsx,.xls"
          onChange={handleFileUpload}
          className="border rounded px-4 py-2 w-full"
        />
        <p className="text-xs text-gray-500 mt-2">
          Descarga el reporte desde: Meta Ads Manager → Campañas → Exportar como Excel
        </p>
      </div>

      {filas.length > 0 && (
        <div className="mb-6">
          <h3 className="text-lg font-semibold mb-4">
            Vista Previa: {filas.length} anuncio(s) encontrado(s)
          </h3>

          <div className="overflow-x-auto">
            <table className="min-w-full border-collapse border border-gray-300">
              <thead className="bg-gray-100">
                <tr>
                  <th className="border p-2 text-left">Anuncio</th>
                  <th className="border p-2 text-left">Período</th>
                  <th className="border p-2 text-right">Importe</th>
                  <th className="border p-2 text-left">Producto</th>
                  <th className="border p-2 text-center">✓</th>
                </tr>
              </thead>
              <tbody>
                {filas.map((fila, idx) => (
                  <tr key={idx} className={!fila.productoId ? 'bg-yellow-50' : ''}>
                    <td className="border p-2 text-sm font-medium">{fila.nombreAnuncio}</td>
                    <td className="border p-2 text-sm">
                      {fila.fechaInicio.toLocaleDateString()} a{' '}
                      {fila.fechaFin.toLocaleDateString()}
                    </td>
                    <td className="border p-2 text-right text-sm">
                      S/. {fila.importeGastado.toFixed(2)}
                    </td>
                    <td className="border p-2">
                      <select
                        value={fila.productoId || ''}
                        onChange={(e) =>
                          handleProductoChange(idx, parseInt(e.target.value))
                        }
                        className="border rounded px-2 py-1 text-sm w-full"
                      >
                        <option value="">-- Selecciona tu producto --</option>
                        {productos?.map(p => (
                          <option key={p.id} value={p.id}>
                            {p.nombre}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="border p-2 text-center">
                      {fila.productoId ? '✓' : '✗'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-6 flex gap-4">
            <button
              onClick={handleConfirmar}
              disabled={loading || filas.some(f => !f.productoId)}
              className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 disabled:opacity-50"
            >
              {loading ? '⏳ Importando...' : '🚀 Confirmar Importación'}
            </button>
            <button
              onClick={() => setFilas([])}
              className="bg-gray-400 text-white px-6 py-2 rounded hover:bg-gray-500"
            >
              ❌ Cancelar
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
```

---

#### Tarea 8: Componente ResumenROI

**Archivo:** `src/presentation/views/Modules/Admin/Views/GastoPublicidad/ResumenRoi.tsx`

```typescript
import React, { useEffect, useState } from 'react';
import { axiosInstance } from '@utils/axios';

interface RoiPorProducto {
  productoId: number;
  nombreProducto: string;
  gastoAds: number;
  ingresos: number;
  costoProducto: number;
  utilidadNeta: number;
  roiPorcentaje: number | null;
  cantidadVendida: number;
  estado: string;
}

interface ResumenRoi {
  datosResumen: {
    gastoTotalAds: number;
    ingresosTotal: number;
    costoProductoTotal: number;
    utilidadNetaTotal: number;
    roiPorcentajePromedio: number;
  };
  porProducto: RoiPorProducto[];
}

export const ResumenRoi = () => {
  const [roi, setRoi] = useState<ResumenRoi | null>(null);
  const [loading, setLoading] = useState(true);
  const [desde, setDesde] = useState(
    new Date(new Date().setDate(new Date().getDate() - 30))
  );
  const [hasta, setHasta] = useState(new Date());

  useEffect(() => {
    cargarRoi();
  }, [desde, hasta]);

  const cargarRoi = async () => {
    setLoading(true);
    try {
      const response = await axiosInstance.get('/api/gastopublicidad/roi', {
        params: {
          desde: desde.toISOString().split('T')[0],
          hasta: hasta.toISOString().split('T')[0]
        }
      });
      setRoi(response.data);
    } catch (error) {
      console.error('Error cargando ROI:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="p-6">Cargando análisis...</div>;
  if (!roi) return <div className="p-6">Sin datos disponibles</div>;

  const { datosResumen, porProducto } = roi;

  return (
    <div className="p-6 bg-white rounded-lg shadow">
      <h1 className="text-2xl font-bold mb-6">📊 ANÁLISIS ROI - PUBLICIDAD META ADS</h1>

      {/* FILTROS */}
      <div className="mb-6 flex gap-4">
        <div>
          <label className="block text-sm font-medium mb-1">Desde</label>
          <input
            type="date"
            value={desde.toISOString().split('T')[0]}
            onChange={(e) => setDesde(new Date(e.target.value))}
            className="border rounded px-3 py-2"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Hasta</label>
          <input
            type="date"
            value={hasta.toISOString().split('T')[0]}
            onChange={(e) => setHasta(new Date(e.target.value))}
            className="border rounded px-3 py-2"
          />
        </div>
      </div>

      {/* RESUMEN TOTAL */}
      <div className="bg-blue-50 border-l-4 border-blue-600 rounded p-6 mb-6">
        <h2 className="font-semibold text-lg mb-4">📈 RESUMEN TOTAL</h2>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
          <div>
            <p className="text-sm text-gray-600">Gasto en Ads</p>
            <p className="text-2xl font-bold">S/. {datosResumen.gastoTotalAds.toFixed(2)}</p>
          </div>
          <div>
            <p className="text-sm text-gray-600">Ingresos (Ventas)</p>
            <p className="text-2xl font-bold">S/. {datosResumen.ingresosTotal.toFixed(2)}</p>
          </div>
          <div>
            <p className="text-sm text-gray-600">Costo Productos</p>
            <p className="text-2xl font-bold">
              S/. {datosResumen.costoProductoTotal.toFixed(2)}
            </p>
          </div>
          <div className="md:col-span-3">
            <p className="text-sm text-gray-600">Utilidad Neta</p>
            <p className="text-3xl font-bold" 
               style={{ color: datosResumen.utilidadNetaTotal >= 0 ? '#10b981' : '#ef4444' }}>
              S/. {datosResumen.utilidadNetaTotal.toFixed(2)}
            </p>
          </div>
          <div className="md:col-span-3">
            <p className="text-sm text-gray-600">ROI Promedio</p>
            <p className="text-3xl font-bold"
               style={{ color: datosResumen.roiPorcentajePromedio >= 0 ? '#10b981' : '#ef4444' }}>
              {(datosResumen.roiPorcentajePromedio * 100).toFixed(1)}%
            </p>
          </div>
        </div>
      </div>

      {/* DESGLOSE POR PRODUCTO */}
      <h2 className="text-lg font-semibold mb-4">📋 RANKING DE ANUNCIOS</h2>
      <div className="space-y-3">
        {porProducto.map((producto, idx) => {
          const bgColor = 
            producto.estado === 'rentable' 
              ? 'bg-green-50 border-green-300'
              : producto.estado === 'perdida'
              ? 'bg-red-50 border-red-300'
              : 'bg-gray-50 border-gray-300';

          return (
            <div
              key={producto.productoId}
              className={`border-l-4 rounded p-4 ${bgColor}`}
            >
              <div className="flex justify-between items-start mb-2">
                <div>
                  <p className="font-bold text-lg">
                    {idx === 0 ? '🥇' : idx === 1 ? '🥈' : idx === 2 ? '🥉' : '📌'} {producto.nombreProducto}
                  </p>
                  <p className="text-sm text-gray-700">
                    {producto.cantidadVendida} unidad{producto.cantidadVendida !== 1 ? 'es' : ''} vendida
                  </p>
                </div>
                <span className="text-2xl font-bold" 
                      style={{ color: producto.roiPorcentaje && producto.roiPorcentaje >= 0 ? '#10b981' : '#ef4444' }}>
                  {producto.roiPorcentaje === null ? '—' : `${(producto.roiPorcentaje * 100).toFixed(1)}%`}
                </span>
              </div>

              <div className="bg-white bg-opacity-50 rounded p-3 mb-2">
                <p className="text-sm text-gray-700">
                  💰 Gasto Ads: <span className="font-semibold">S/. {producto.gastoAds.toFixed(2)}</span> | 
                  📊 Ingresos: <span className="font-semibold">S/. {producto.ingresos.toFixed(2)}</span> | 
                  📦 Costo: <span className="font-semibold">S/. {producto.costoProducto.toFixed(2)}</span>
                </p>
              </div>

              <p className="text-sm font-semibold">
                💵 Utilidad Neta: 
                <span style={{ color: producto.utilidadNeta >= 0 ? '#10b981' : '#ef4444' }} className="ml-2">
                  S/. {producto.utilidadNeta.toFixed(2)}
                </span>
              </p>

              {idx === 0 && <p className="text-xs mt-2 text-green-700">✅ RECOMENDACIÓN: Escala este anuncio</p>}
              {idx === porProducto.length - 1 && producto.estado === 'perdida' && 
               <p className="text-xs mt-2 text-red-700">⚠️ RECOMENDACIÓN: Revisa o pausa este anuncio</p>}
            </div>
          );
        })}
      </div>

      <div className="mt-6 flex gap-2">
        <button
          onClick={() => window.location.reload()}
          className="bg-gray-600 text-white px-4 py-2 rounded hover:bg-gray-700 text-sm"
        >
          🔄 Recargar
        </button>
      </div>
    </div>
  );
};
```

---

#### Tarea 8b: Agregar al Menú Principal del POS

**Archivo:** `src/presentation/layouts/MenuPrincipal.tsx` o similar

Agregar en el menú:

```jsx
<MenuItem 
  icon={<AnalyticsIcon />} 
  label="ROI Publicidad" 
  path="/admin/gasto-publicidad"
/>
```

---

## ✅ CHECKLIST SPRINT 1

**Backend:**
- [ ] Modelo `GastoPublicidad.cs` creado
- [ ] Migración EF Core aplicada
- [ ] `IGastoPublicidadRepository` interfaz
- [ ] `GastoPublicidadRepository` implementado
- [ ] DTOs creados
- [ ] `GastoPublicidadController` endpoints
- [ ] Dependency injection configurado
- [ ] Tests básicos ROI ~80% cobertura

**Frontend:**
- [ ] Componente `FileUploadPublicidad.tsx`
- [ ] Componente `ResumenRoi.tsx`
- [ ] Link en menú principal del POS
- [ ] Routing funcionando (`/admin/gasto-publicidad`)
- [ ] Upload file → preview → confirm → display ROI

**QA:**
- [ ] Testeado con Excel real de Meta Ads
- [ ] Validación: fechas coherentes
- [ ] Validación: producto existe
- [ ] Validación: importe >= 0
- [ ] ROI calculado correcto (verificar manualmente)
- [ ] Histórico guardado en BD
- [ ] Multi-tenant aislado (no ver datos de otro tenant)

---

## 🎬 PARA EMPEZAR

1. **Backend dev:**
   - Crea archivos Tareas 1-6b (modelo, migración, repo, DTOs, controller)
   - Ejecuta migración
   - Configura dependency injection

2. **Frontend dev:**
   - Crea archivos Tareas 7-8b (componentes React)
   - Conecta con endpoints del backend
   - Integra en menú principal

3. **QA:**
   - Descarga Excel real de Meta Ads Manager
   - Prueba flujo completo: upload → mapeo → cálculo → visualización
   - Valida que ROI coincida con cálculo manual

**Tiempo estimado:** 5-7 días dev + 2 días QA

¿Dudas sobre el código? Lee el documento de especificación técnica (`ROI_Publicidad_Design_Spec_Mejorado.md`). Contiene todas las respuestas.

**Listos para empezar lunes? 🚀**
