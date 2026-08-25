# ROI de Publicidad de Facebook — Design Spec MEJORADA & EJECUTABLE

**Fecha:** 2026-08-25  
**Estado:** Aprobado para desarrollo inmediato  
**Estimado:** 3-4 sprints (2-3 semanas por sprint)

---

## 🎯 RESUMEN EJECUTIVO

### Problema
El usuario no sabe cuál anuncio en Facebook genera ROI real. Hoy:
- ✗ No vincula gasto en ads con ventas reales del producto
- ✗ No calcula margen considerando costo del producto
- ✗ No puede optimizar presupuesto publicitario

### Solución
Sistema que:
1. Lee Excel de Meta Ads Manager
2. Vincula cada anuncio a un producto
3. Calcula ROI real: `(Ingresos - Costo Producto - Gasto Ads) / Gasto Ads`
4. Muestra qué anuncios son rentables

### Impacto Esperado
- ✅ Usuario identifica en 5 minutos qué anuncios pausar/escalar
- ✅ Reduce desperdicio publicitario ~20-30%
- ✅ Mejora margen neto del negocio

---

## 📅 ROADMAP POR SPRINTS

### SPRINT 1 (Semana 1-2): MVP Core
**Objetivo:** Importar Excel y calcular ROI básico

**Tareas:**
- [ ] Crear modelo `GastoPublicidad` con migraciones
- [ ] Endpoint `/api/gastopublicidad/importar` (sin validaciones avanzadas)
- [ ] Endpoint `/api/gastopublicidad/roi` (cálculo básico)
- [ ] UI: File upload + tabla preview + mapeo manual anuncio→producto
- [ ] Tests básicos de cálculo ROI

**Criterio de aceptación:**
- Usuario sube Excel Meta → ve tabla con ROI % por producto
- Validación: fechas coherentes, producto existe, importe >= 0

---

### SPRINT 2 (Semana 3-4): Funciones Críticas
**Objetivo:** Hacerlo usable en producción

**Tareas:**
- [ ] Detección de duplicatas (hash por anuncio)
- [ ] Auto-matching fuzzy: nombre anuncio → producto recomendado
- [ ] Historial persistente: filtrar importaciones anteriores
- [ ] Validación robusta de Excel (columnas faltantes, tipos de dato)
- [ ] Manejo de overlaps de fechas (advertencia si se solapan anuncios)
- [ ] Tests exhaustivos (100+ casos)

**Criterio de aceptación:**
- Sistema rechaza Excel malformados con mensajes claros
- Usuario no puede duplicar una importación por accidente
- Auto-matching sugiere el producto correcto >80% de las veces

---

### SPRINT 3 (Semana 5-6): Inteligencia de Negocio
**Objetivo:** Recomendaciones automáticas y análisis

**Tareas:**
- [ ] Endpoint `/api/gastopublicidad/tendencias` (ROI por período)
- [ ] Detectar anomalías: CPC alto, ROI negativo, cambios repentinos
- [ ] Integración Claude API: generar recomendaciones automáticas
- [ ] Gráficos de tendencia (Recharts)
- [ ] Comparativa de anuncios (A/B)

**Criterio de aceptación:**
- Usuario ve gráfico de ROI en últimos 90 días
- Sistema sugiere: pausar anuncio X, escalar anuncio Y
- Alertas ante cambios anómalos

---

### SPRINT 4 (Futuro): Polish & Escalabilidad
- [ ] Exportar reportes PDF profesionales
- [ ] Soporte Google Ads (mismo patrón, distinto parser)
- [ ] Soporte TikTok Ads
- [ ] Dashboard ejecutivo para managers
- [ ] Notificaciones automáticas (email cuando ROI cae <0)

---

## 📊 MODELO DE DATOS (Detallado)

### Entidad: `GastoPublicidad`

```csharp
public class GastoPublicidad : EntityBase
{
    // Relación con producto
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    // Datos del anuncio (Meta Ads Manager)
    public string NombreAnuncio { get; set; } = null!;
    public string? NombreConjuntoAnuncios { get; set; }

    // Período del anuncio
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    // Gastos e impacto
    public decimal ImporteGastado { get; set; }  // Siempre en PEN
    
    // Métricas de Meta (opcionales, solo referencia)
    public int? Impresiones { get; set; }
    public int? Alcance { get; set; }
    public int? Resultados { get; set; }  // Compras atribuidas por Meta
    public decimal? CostoPorResultado { get; set; }

    // Agrupación
    public Guid LoteImportacionId { get; set; }

    // ===== NUEVOS CAMPOS (SPRINT 2+) =====
    
    // Anti-duplicatas
    public string HashAnuncio { get; set; } = null!;  
    // SHA256(NombreAnuncio + FechaInicio + FechaFin)
    public bool EsDuplicada { get; set; } = false;

    // Anomalías
    public bool TieneAnomalia { get; set; } = false;
    public string? TipoAnomalia { get; set; }  
    // Ej: "CPC_ALTO", "ROI_NEGATIVO", "CAMBIO_ABRUPTO"

    // Métricas calculadas (cachéadas)
    public decimal? CPC { get; set; }  // Importe / Impresiones
    public decimal? ROAS { get; set; }  // ROI real calculado
}
```

### Índices Base de Datos

```sql
-- Para búsquedas rápidas
CREATE INDEX idx_gasto_publicidad_producto_fecha 
    ON GastoPublicidad(ProductoId, FechaInicio, FechaFin);

CREATE INDEX idx_gasto_publicidad_lote 
    ON GastoPublicidad(LoteImportacionId);

CREATE INDEX idx_gasto_publicidad_hash 
    ON GastoPublicidad(HashAnuncio);

-- Para tenant isolation
CREATE INDEX idx_gasto_publicidad_tenant 
    ON GastoPublicidad(TenantId, FechaInicio);
```

---

## 🔧 BACKEND: ENDPOINTS DETALLADOS

### 1. `POST /api/gastopublicidad/importar`

**Request:**
```json
{
  "loteImportacionId": "550e8400-e29b-41d4-a716-446655440000",
  "filas": [
    {
      "productoId": 12,
      "nombreAnuncio": "Casacas Impermeables - Men",
      "nombreConjuntoAnuncios": "Ropa Deportiva",
      "fechaInicio": "2026-08-01T00:00:00Z",
      "fechaFin": "2026-08-15T23:59:59Z",
      "importeGastado": 150.50,
      "impresiones": 12000,
      "alcance": 8000,
      "resultados": 34,
      "costoPorResultado": 4.42
    },
    {
      "productoId": 45,
      "nombreAnuncio": "Casacas Azul Deportiva",
      "nombreConjuntoAnuncios": "Ropa Deportiva",
      "fechaInicio": "2026-08-01T00:00:00Z",
      "fechaFin": "2026-08-15T23:59:59Z",
      "importeGastado": 200.00,
      "impresiones": 15000,
      "alcance": 10000,
      "resultados": 22,
      "costoPorResultado": 9.09
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "loteImportacionId": "550e8400-e29b-41d4-a716-446655440000",
  "filasInsertadas": 2,
  "filasRechazadas": 0,
  "alertas": []
}
```

**Response (400 Bad Request):**
```json
{
  "success": false,
  "error": "Validación fallida",
  "detalles": [
    {
      "fila": 0,
      "nombreAnuncio": "Casacas Impermeables - Men",
      "errores": [
        "ProductoId 999 no existe",
        "FechaFin debe ser >= FechaInicio"
      ]
    }
  ]
}
```

**Validaciones:**
- ✅ `ProductoId` existe y pertenece al tenant actual
- ✅ `FechaFin >= FechaInicio`
- ✅ `ImporteGastado >= 0`
- ✅ No hay duplicatas (si las hay, advertir sin bloquear)
- ✅ Transacción: todo o nada

---

### 2. `GET /api/gastopublicidad/roi?desde=2026-08-01&hasta=2026-08-31&productoId=12`

**Parámetros:**
- `desde`: DateTime (ISO 8601, ej: `2026-08-01T00:00:00Z`)
- `hasta`: DateTime (ISO 8601)
- `productoId`: int (opcional; si se omite, agrupa por todos los productos)

**Response:**
```json
{
  "datosResumen": {
    "gastoTotalAds": 350.50,
    "ingresosTotal": 2890.00,
    "costoProductoTotal": 1410.00,
    "utilidadNetaTotal": 1128.50,
    "roiPorcentajePromedio": 3.22
  },
  "porProducto": [
    {
      "productoId": 12,
      "nombreProducto": "Casaca Impermeable Hombre",
      "gastoAds": 150.50,
      "ingresos": 1200.00,
      "costoProducto": 600.00,
      "utilidadNeta": 449.50,
      "roiPorcentaje": 2.98,
      "estado": "rentable",
      "cantidadVendida": 15
    },
    {
      "productoId": 45,
      "nombreProducto": "Casaca Azul Deportiva",
      "gastoAds": 200.00,
      "ingresos": 1690.00,
      "costoProducto": 810.00,
      "utilidadNeta": 680.00,
      "roiPorcentaje": 3.40,
      "estado": "rentable",
      "cantidadVendida": 22
    }
  ]
}
```

**Lógica de Cálculo (en `GastoPublicidadRepository`):**

```csharp
public async Task<RoiAnalysisDto> CalcularRoi(
    DateTime desde, DateTime hasta, int? productoId = null)
{
    // 1. Gastos en publicidad
    var gastosQuery = _context.GastoPublicidad
        .Where(g => g.FechaInicio >= desde && g.FechaFin <= hasta);
    
    if (productoId.HasValue)
        gastosQuery = gastosQuery.Where(g => g.ProductoId == productoId);

    // 2. Ingresos por ventas (dentro del rango del anuncio)
    // Para cada anuncio, buscar comprobantes dentro de FechaInicio-FechaFin
    var resultados = await gastosQuery
        .GroupBy(g => g.ProductoId)
        .Select(grupo => new RoiPorProductoDto
        {
            ProductoId = grupo.Key,
            NombreProducto = grupo.First().Producto.Nombre,
            
            GastoAds = grupo.Sum(g => g.ImporteGastado),
            
            // Ingresos: suma de ValorUnitarioTotal en ComprobantesDetalle
            // del producto, dentro del rango de CADA anuncio
            Ingresos = _context.ComprobanteDetalles
                .Where(d => d.ProductoId == grupo.Key
                        && grupo.Any(g => 
                            d.ComprobanteCabecera.FechaCreacion >= g.FechaInicio
                            && d.ComprobanteCabecera.FechaCreacion <= g.FechaFin)
                        && d.ComprobanteCabecera.EstadoComprobante 
                            != EstatusComprobante.Anulado)
                .Sum(d => (decimal?)d.ValorUnitarioTotal) ?? 0,
            
            // Costo: cantidad * costo unitario actual
            CostoProducto = _context.ComprobanteDetalles
                .Where(d => d.ProductoId == grupo.Key
                        && grupo.Any(g => 
                            d.ComprobanteCabecera.FechaCreacion >= g.FechaInicio
                            && d.ComprobanteCabecera.FechaCreacion <= g.FechaFin)
                        && d.ComprobanteCabecera.EstadoComprobante 
                            != EstatusComprobante.Anulado)
                .Sum(d => (decimal?)(d.Cantidad 
                    * (d.Producto.CostoUnitario ?? 0))) ?? 0,
            
            CantidadVendida = _context.ComprobanteDetalles
                .Where(d => d.ProductoId == grupo.Key
                        && grupo.Any(g => 
                            d.ComprobanteCabecera.FechaCreacion >= g.FechaInicio
                            && d.ComprobanteCabecera.FechaCreacion <= g.FechaFin)
                        && d.ComprobanteCabecera.EstadoComprobante 
                            != EstatusComprobante.Anulado)
                .Sum(d => (int?)d.Cantidad) ?? 0
        })
        .ToListAsync();

    // 3. Calcular utilidad y ROI
    var conCalculos = resultados.Select(r => new RoiPorProductoDto
    {
        ProductoId = r.ProductoId,
        NombreProducto = r.NombreProducto,
        GastoAds = r.GastoAds,
        Ingresos = r.Ingresos,
        CostoProducto = r.CostoProducto,
        CantidadVendida = r.CantidadVendida,
        UtilidadNeta = r.Ingresos - r.CostoProducto - r.GastoAds,
        RoiPorcentaje = r.GastoAds > 0 
            ? (r.Ingresos - r.CostoProducto - r.GastoAds) / r.GastoAds 
            : null,
        Estado = DeterminarEstado(/* lógica */)
    }).ToList();

    return new RoiAnalysisDto
    {
        DatosResumen = new ResumenRoiDto
        {
            GastoTotalAds = conCalculos.Sum(r => r.GastoAds),
            IngresosTotal = conCalculos.Sum(r => r.Ingresos),
            CostoProductoTotal = conCalculos.Sum(r => r.CostoProducto),
            UtilidadNetaTotal = conCalculos.Sum(r => r.UtilidadNeta),
            RoiPorcentajePromedio = conCalculos.Average(r => r.RoiPorcentaje ?? 0)
        },
        PorProducto = conCalculos
    };
}
```

---

### 3. `GET /api/gastopublicidad?page=1&pageSize=10&productoId=12&desde=2026-08-01`

**Historial de importaciones (paginado)**

**Response:**
```json
{
  "items": [
    {
      "id": 1,
      "productoId": 12,
      "nombreProducto": "Casaca Impermeable Hombre",
      "nombreAnuncio": "Casacas Impermeables - Men",
      "fechaInicio": "2026-08-01T00:00:00Z",
      "fechaFin": "2026-08-15T23:59:59Z",
      "importeGastado": 150.50,
      "loteImportacionId": "550e8400-e29b-41d4-a716-446655440000",
      "esDuplicada": false,
      "tieneAnomalia": false
    }
  ],
  "total": 42,
  "page": 1,
  "pageSize": 10
}
```

---

### 4. `POST /api/gastopublicidad/validar-duplicatas` (SPRINT 2)

**Request:**
```json
{
  "filas": [
    {
      "nombreAnuncio": "Casacas Impermeables - Men",
      "fechaInicio": "2026-08-01",
      "fechaFin": "2026-08-15",
      "importeGastado": 150.50
    }
  ]
}
```

**Response:**
```json
{
  "duplicadas": [],
  "advertencias": []
}
```

---

### 5. `POST /api/gastopublicidad/sugerir-productos` (SPRINT 2)

**Request:**
```json
{
  "nombreAnuncio": "Casacas Impermeables - Men",
  "nombreConjuntoAnuncios": "Ropa Deportiva"
}
```

**Response:**
```json
{
  "sugerencias": [
    {
      "productoId": 12,
      "nombre": "Casaca Impermeable Hombre",
      "confianza": 0.95,
      "razon": "Coincidencia de palabras clave: 'Casaca', 'Impermeable'"
    },
    {
      "productoId": 45,
      "nombre": "Casaca Deportiva Azul",
      "confianza": 0.72
    }
  ]
}
```

**Implementación (usar librería `fuse.js`):**

```csharp
public async Task<List<ProductoSugerenciaDto>> SugerirProductos(
    string nombreAnuncio)
{
    var productos = await _context.Productos
        .Where(p => p.TenantId == _tenantId && p.Activo)
        .ToListAsync();

    // Usar fuzzy matching: cada palabra del anuncio en el nombre del producto
    var palabrasAnuncio = nombreAnuncio.ToLower().Split(' ');
    
    var sugerencias = productos
        .Select(p => new
        {
            Producto = p,
            Confianza = CalcularSimilitud(nombreAnuncio, p.Nombre)
        })
        .Where(s => s.Confianza >= 0.5)  // Al menos 50%
        .OrderByDescending(s => s.Confianza)
        .Take(3)
        .Select(s => new ProductoSugerenciaDto
        {
            ProductoId = s.Producto.Id,
            Nombre = s.Producto.Nombre,
            Confianza = s.Confianza
        })
        .ToList();

    return sugerencias;
}

private decimal CalcularSimilitud(string str1, string str2)
{
    // Usar algoritmo Levenshtein simplificado
    // o librería externa como: "LevenshteinDistance" NuGet
    // Retorna valor 0.0 - 1.0
}
```

---

## 🎨 FRONTEND: FLUJO UI

### Pantalla 1: Upload & Preview

```
┌─ ROI Publicidad (Meta Ads) ──────────────────────────────────┐
│                                                               │
│  [📁 Subir archivo Excel] → "facebook_ads_2026_08.xlsx"      │
│                                                               │
│  ✅ Archivo parseado: 5 anuncios encontrados                │
│  ⚠️  1 anuncio descartado (Importe gastado vacío)            │
│                                                               │
│  ┌─ Vista Previa ─────────────────────────────────────────┐  │
│  │ Anuncio                │ Importe  │ Producto      │ ✓   │  │
│  ├─────────────────────────┼──────────┼───────────────┼─────┤  │
│  │ Casacas Impermeables   │ $150.50  │ [▼ Casaca...] │ ✓   │  │
│  │ Casaca Azul Deportiva  │ $200.00  │ [▼ No asignado]│ ✗  │  │
│  │ Ropa Deportiva - Hombre│ $120.00  │ [▼ Casaca...] │ ✓   │  │
│  └─────────────────────────┴──────────┴───────────────┴─────┘  │
│                                                               │
│  (si detecta duplicatas)                                     │
│  ⚠️  2 anuncios ya fueron importados el 2026-08-20           │
│     [✓ Actualizar] [✗ Ignorar] [❌ Cancelar]                │
│                                                               │
│  [🚀 Confirmar Importación]  [❌ Cancelar]                   │
└─────────────────────────────────────────────────────────────┘
```

**Componentes React:**

```jsx
// FILE: FileUploadPublicidad.tsx
import XLSX from 'xlsx';
import { useFuzzyMatch } from '../hooks/useFuzzyMatch';

export const FileUploadPublicidad = () => {
  const [filas, setFilas] = useState([]);
  const { sugerirProducto } = useFuzzyMatch();

  const handleFileUpload = async (file) => {
    const workbook = XLSX.read(file, { type: 'array' });
    const sheet = workbook.Sheets[workbook.SheetNames[0]];
    const data = XLSX.utils.sheet_to_json(sheet);

    // Validar columnas requeridas
    const columnasRequeridas = [
      'Inicio del informe',
      'Fin del informe',
      'Nombre del anuncio',
      'Importe gastado (PEN)'
    ];

    const tieneTodasLasColumnas = columnasRequeridas.every(col => 
      data[0]?.hasOwnProperty(col)
    );

    if (!tieneTodasLasColumnas) {
      toast.error('Excel no tiene las columnas requeridas');
      return;
    }

    // Parsear y mapear
    const filasParseadas = data
      .filter(row => row['Importe gastado (PEN)']) // Descartar filas vacías
      .map(row => ({
        nombreAnuncio: row['Nombre del anuncio'],
        nombreConjuntoAnuncios: row['Nombre del conjunto de anuncios'],
        fechaInicio: new Date(row['Inicio del informe']),
        fechaFin: new Date(row['Fin del informe']),
        importeGastado: parseFloat(row['Importe gastado (PEN)']),
        impresiones: row['Impresiones'],
        alcance: row['Alcance'],
        resultados: row['Compras'],
        costoPorResultado: row['Costo por compra (PEN)'],
        productoId: null,
        sugerencias: await sugerirProducto(row['Nombre del anuncio'])
      }));

    setFilas(filasParseadas);
  };

  return (
    <div>
      <input 
        type="file" 
        accept=".xlsx" 
        onChange={(e) => handleFileUpload(e.target.files[0])}
      />
      
      {filas.length > 0 && (
        <FilasPreview filas={filas} onProductoChange={handleProductoChange} />
      )}
    </div>
  );
};
```

---

### Pantalla 2: Resumen ROI

```
┌─ ROI por Producto (2026-08-01 a 2026-08-31) ──────────────────┐
│                                                                 │
│  Filtros: [Desde 2026-08-01] [Hasta 2026-08-31] [Producto: Todos▼]
│                                                                 │
│  📊 RESUMEN TOTAL                                              │
│  ┌────────────────────────────────────────────────────────┐   │
│  │ Gasto Ads: $350.50  │ Ingresos: $2,890  │ ROI: 324%   │   │
│  │ Costo Producto: $1,410  │ Utilidad Neta: $1,128.50    │   │
│  └────────────────────────────────────────────────────────┘   │
│                                                                 │
│  📋 DESGLOSE POR PRODUCTO                                      │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │ Casaca Impermeable Hombre                   │ ✅ 298%  │  │
│  │ Gasto: $150.50 | Ingresos: $1,200 | Margen Neto: 67%  │  │
│  ├─────────────────────────────────────────────────────────┤  │
│  │ Casaca Azul Deportiva                      │ ✅ 340%  │  │
│  │ Gasto: $200 | Ingresos: $1,690 | Margen Neto: 75%     │  │
│  ├─────────────────────────────────────────────────────────┤  │
│  │ Ropa Deportiva - Mujer                     │ ❌ -45% │  │
│  │ Gasto: $120 | Ingresos: $180 | Margen Neto: -50%      │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                 │
│  [💾 Descargar Reporte] [📊 Ver Tendencias] [➕ Nuevo Import] │
└─────────────────────────────────────────────────────────────┘
```

---

## 🧪 TESTING: Plan Exhaustivo

### Unit Tests - Backend

**`GastoPublicidadRepositoryTests.cs`**

```csharp
[TestClass]
public class CalcularRoiTests
{
    private GastoPublicidadRepository _repo;
    private Mock<DataContext> _mockContext;

    [TestInitialize]
    public void Setup()
    {
        _mockContext = new Mock<DataContext>();
        _repo = new GastoPublicidadRepository(_mockContext.Object);
    }

    [TestMethod]
    public async Task CalcularRoi_ConVentasCompletas_RetornaRoiPositivo()
    {
        // Arrange: producto 12, anuncio del 1-15 agost, gasto $150
        var gasto = new GastoPublicidad
        {
            ProductoId = 12,
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 15),
            ImporteGastado = 150m,
            Producto = new Producto { Id = 12, Nombre = "Casaca", CostoUnitario = 40m }
        };

        // Ventas dentro del período: 15 unidades a $80 c/u = $1200
        var comprobante = new Comprobante { FechaCreacion = new DateTime(2026, 8, 10) };
        var detalles = Enumerable.Range(1, 15)
            .Select(i => new ComprobanteDetalle
            {
                ProductoId = 12,
                Cantidad = 1,
                ValorUnitarioTotal = 80m,
                Producto = gasto.Producto,
                ComprobanteCabecera = comprobante
            }).ToList();

        // Mock el contexto
        _mockContext.Setup(c => c.GastoPublicidad)
            .Returns(DbSetMock.Create(new[] { gasto }));
        _mockContext.Setup(c => c.ComprobanteDetalles)
            .Returns(DbSetMock.Create(detalles));

        // Act
        var resultado = await _repo.CalcularRoi(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 31),
            productoid: 12
        );

        // Assert
        var roi = resultado.PorProducto[0];
        Assert.AreEqual(150m, roi.GastoAds);
        Assert.AreEqual(1200m, roi.Ingresos);
        Assert.AreEqual(600m, roi.CostoProducto);  // 15 * 40
        Assert.AreEqual(450m, roi.UtilidadNeta);   // 1200 - 600 - 150
        Assert.AreEqual(3.0m, roi.RoiPorcentaje);  // 450 / 150
    }

    [TestMethod]
    public async Task CalcularRoi_ExcluyeComprobanteAnulados()
    {
        // Similiar al anterior, pero con comprobantes anulados
        // Verificar que no se incluyen en cálculo
    }

    [TestMethod]
    public async Task CalcularRoi_SinVentasEnRango_RetornaRoiNegativo()
    {
        // Gasto $150 pero 0 ventas en ese período
        // Esperado: utilidad = -$150, ROI = -100%
    }

    [TestMethod]
    public async Task CalcularRoi_GastoAds_CeroDividePorCero_RetornaNull()
    {
        // Si ImporteGastado = 0, ROI debe ser null, no infinito
    }
}

[TestClass]
public class ValidarImportTests
{
    [TestMethod]
    public async Task ValidarImport_ProductoNoExiste_RetornaError()
    {
        var payload = new { productoId = 999 };
        var resultado = await _repo.Importar(payload);
        
        Assert.IsFalse(resultado.Success);
        Assert.IsTrue(resultado.Errores.Any(e => e.Contains("no existe")));
    }

    [TestMethod]
    public async Task ValidarImport_FechaFinAntesDeFechaInicio_RetornaError()
    {
        var payload = new { 
            fechaInicio = new DateTime(2026, 8, 31),
            fechaFin = new DateTime(2026, 8, 1)
        };
        var resultado = await _repo.Importar(payload);
        
        Assert.IsFalse(resultado.Success);
    }

    [TestMethod]
    public async Task DetectarDuplicatas_MismoAnuncio_TwiceEnMesmo_AlertaYNoInserta()
    {
        // Subir dos veces el mismo lote
        // Esperado: detecta duplicata, propone actualizar o ignorar
    }
}
```

### E2E Tests - Frontend

**`FileUploadPublicidad.e2e.spec.ts`**

```typescript
describe('File Upload Publicidad', () => {
  it('should parse Meta Ads Excel correctly', async () => {
    // 1. Subir archivo real de Meta
    // 2. Verificar que se detectan correctamente los anuncios
    // 3. Verificar que sugiere productos correctos
  });

  it('should show validation errors for malformed Excel', () => {
    // Excel sin columnas requeridas
    // Debe mostrar: "Falta columna 'Importe gastado (PEN)'"
  });

  it('should prevent submit if any row has unassigned producto', () => {
    // Una fila sin producto seleccionado
    // Botón "Confirmar" debe estar disabled
    // Al asignar, botón debe habilitarse
  });
});
```

### Manual Verification Checklist

- [ ] Descargar Excel real de Meta Ads Manager
- [ ] Subir a la aplicación
- [ ] Comparar ROI mostrado con cálculo manual en Excel
- [ ] Verificar que excluye comprobantes anulados
- [ ] Verificar que toma el costo actual del producto, no histórico
- [ ] Verificar overlap de fechas (si 2 anuncios se solapan, ¿se cuentan las ventas en ambos?)

---

## 🚨 DECISIONES CRÍTICAS (Confirmadas)

| Decisión | Valor | Razón |
|----------|-------|-------|
| **Cálculo de costo** | Costo actual, no histórico | Simplifica modelo; cliente acepta aproximación |
| **Ventas en overlap** | Se cuentan en ambos anuncios | Simplificación aceptada; futuro: atribución más sofisticada |
| **Mapeo anuncio→producto** | Manual + sugerencias fuzzy | Auto-match puro da falsos positivos altos |
| **Divisibilidad ROI=0** | Retorna null | Frontend maneja sin division by zero |
| **Persistencia** | Sí, se guarda historial completo | Permite auditoría y análisis temporal |

---

## ⚠️ LIMITACIONES CONOCIDAS

1. **Costo histórico**: Si el `CostoUnitario` del producto cambió durante el período del anuncio, el ROI es aproximado (usa costo *actual*).
   - **Futuro**: Agregar snapshot de costo al momento de la compra.

2. **Atribución**: Asume que todas las ventas en el rango de fechas se deben al anuncio (no es cierto; otros canales contribuyen).
   - **Futuro**: Modelo de atribución multi-touch (first-click, last-click, linear, etc.).

3. **Sin soporte multi-moneda**: Todo en PEN. Meta podría traer en USD.
   - **Futuro**: Detectar y convertir automáticamente.

4. **Meta Ads solamente**: Google Ads, TikTok Ads vienen después (mismo patrón).

---

## 📦 DEPENDENCIAS (Validar disponibilidad)

### Backend
- ✅ EF Core (ya en proyecto)
- ✅ Logging (ya en proyecto)
- ✅ AutoMapper (ya en proyecto)
- ⚠️ String similarity (NuGet: `LevenshteinDistance` o `FuzzySharp`)
- ⚠️ SHA256 (System.Security.Cryptography - built-in)

### Frontend
- ✅ `xlsx` (ya instalado)
- ✅ Recharts (para gráficos, si no está: `npm install recharts`)
- ⚠️ `fuse.js` (para fuzzy matching: `npm install fuse.js`)

### Infraestructura
- ✅ Base de datos existente
- ⚠️ Claude API key (si implementas recomendaciones en Sprint 3)

---

## 📋 CHECKLIST FINAL

**Antes de empezar SPRINT 1:**
- [ ] ¿Tenemos acceso a un Excel real de Meta Ads Manager?
- [ ] ¿QA tiene Meta account para testear?
- [ ] ¿Confirmado el patrón de cálculo con el usuario?
- [ ] ¿Arquitectura multi-tenant está clara?

**Fin SPRINT 1 - Criteria de aceptación:**
- [ ] MVP funciona end-to-end (upload → preview → confirm → ROI)
- [ ] Validaciones básicas OK
- [ ] Tests al 80%
- [ ] Sin crashear con Excel real de Meta

**Fin SPRINT 2 - Criteria de aceptación:**
- [ ] Detección de duplicatas 100%
- [ ] Auto-matching fuzzy >80% precisión
- [ ] 0 regresiones de Sprint 1
- [ ] Documentación de usuario completada

---

**¿Listos para empezar?** 🚀
