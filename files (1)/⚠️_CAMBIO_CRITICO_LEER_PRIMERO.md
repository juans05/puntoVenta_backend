# ⚠️ CAMBIO CRÍTICO - Lee esto PRIMERO

## ¿Qué cambió?

El proyecto **NO es un SaaS separado** vendido por suscripción.

Es una **FEATURE INTEGRADA en el POS existente**, como si fuera un nuevo módulo del menú.

---

## ANTES (Versión Original - DESCARTA)

```
❌ Producto separado
❌ Pricing: $9-15/mes por usuario
❌ SaaS con signup independiente
❌ Proyecciones de adopción "en el mercado"
❌ Soft launch con 20 beta users
❌ Base de datos nueva "ROI_Publicidad"
```

---

## AHORA (Versión Corregida - USA ESTA)

```
✅ Feature integrada en POS existente
✅ INCLUIDA en el plan que ya pagan
✅ Opción nueva en menú principal del POS: [ROI PUBLICIDAD]
✅ Usa la misma BD del POS (tabla GastoPublicidad + tablas existentes)
✅ Usa la misma autenticación del POS
✅ Lanzamiento: Cuando esté listo (no "soft launch")
✅ Adopción: Automática para todos los usuarios del POS
```

---

## IMPACTO EN LOS DOCUMENTOS

### ✅ USAR ESTOS DOCUMENTOS CORREGIDOS:

1. **`RESUMEN_EJECUTIVO_CORREGIDO_Feature_POS.md`**
   - Reemplaza al viejo resumen ejecutivo
   - Sin pricing, sin SaaS, sin adopción por usuario
   - Focus: Diferenciación vs competencia, mejor NPS, reducción churn

2. **`PROMPT_EJECUTABLE_CORREGIDO_Feature_POS.md`**
   - Reemplaza al viejo prompt para developers
   - Claramente integrado en POS existente
   - Mismo código, pero con contexto correcto

3. **`ROI_Publicidad_Design_Spec_Mejorado.md`**
   - Este NO cambió (la especificación técnica es la misma)
   - Solo agregar nota: "Feature del POS, no SaaS"

### ❌ NO USES ESTOS (DESCARTA):

- `RESUMEN_EJECUTIVO_Stakeholders.md` (viejo, asumía SaaS)
- `PROMPT_EJECUTABLE_Developers.md` (viejo, asumía SaaS)
- `COMO_USAR_ESTOS_DOCUMENTOS.md` (viejo, referencia SaaS)

---

## DIFERENCIA CLAVE

### ANTES: SaaS
```
┌─────────────────────┐
│  NUEVA APP (ROI)    │  ← Producto separado
│  Precio: $9-15/mes  │
└─────────────────────┘
        +
┌─────────────────────┐
│  POS (Existente)    │  ← Producto existente
│  Precio: Ya pagan   │
└─────────────────────┘
```

### AHORA: Feature Integrada
```
┌────────────────────────────┐
│    PUNTO DE VENTA          │
│                            │
│  ├─ Dashboard              │
│  ├─ Productos              │
│  ├─ Ventas                 │
│  ├─ ⭐ ROI PUBLICIDAD      │ ← NUEVA FEATURE
│  └─ Configuración          │
│                            │
│  Precio: Incluido          │
└────────────────────────────┘
```

---

## CAMBIOS ESPECÍFICOS EN EL CÓDIGO

### Base de Datos

**ANTES:** Tabla nueva en BD separada
```
Database1 (ROI_PublicidadDB)
├─ GastoPublicidad
├─ Usuario_ROIPublicidad
└─ ConfiguracionROI
```

**AHORA:** Tabla nueva en la BD existente del POS
```
Database_POS (Existente)
├─ Producto (existente)
├─ Comprobante (existente)
├─ ComprobanteDetalle (existente)
├─ Usuario (existente)
├─ Tenant (existente, multi-tenant)
└─ GastoPublicidad ← NUEVA (hereda de EntityBase)
```

### Autenticación

**ANTES:** Login separado para ROI app
```
Usuario abre browser
    ↓
Va a roi.app.com
    ↓
Ingresa usuario/contraseña
    ↓
Accede a dashboard ROI
```

**AHORA:** Usa autenticación existente del POS
```
Usuario abre POS (ya autenticado)
    ↓
Ve menú: [Dashboard] [Productos] [Ventas] [ROI PUBLICIDAD]
    ↓
Hago click en "ROI PUBLICIDAD"
    ↓
Accede a feature ROI (misma sesión, mismo usuario)
```

### Datos

**ANTES:** Datos de ROI completamente separados
**AHORA:** Datos de ROI en la BD compartida del POS
- Ventas reales: Toma de `ComprobanteDetalle` existente
- Costos: Toma de `Producto.CostoUnitario` existente
- Usuario/Tenant: Heredado de `EntityBase` automáticamente

---

## LO QUE NO CAMBIÓ

✅ **El cálculo de ROI es idéntico:**
```
ROI = (Ingresos - CostoProducto - GastoAds) / GastoAds
```

✅ **Los endpoints son idénticos:**
- POST /api/gastopublicidad/importar
- GET /api/gastopublicidad/roi
- GET /api/gastopublicidad

✅ **La arquitectura general es idéntica:**
- Backend: Controller → Repository → BD
- Frontend: React component upload → preview → confirm → display

✅ **El flujo de usuario es idéntico:**
- Upload Excel → Mapear anuncios → Calcular → Ver resultados

---

## CHECKLIST: ANTES DE EMPEZAR DEV TEAM

- [ ] ¿Leí `RESUMEN_EJECUTIVO_CORREGIDO_Feature_POS.md`?
- [ ] ¿Leí `PROMPT_EJECUTABLE_CORREGIDO_Feature_POS.md`?
- [ ] ¿Entiendo que esto NO es un SaaS?
- [ ] ¿Entiendo que se integra en el menú del POS?
- [ ] ¿Entiendo que usa la BD existente del POS?
- [ ] ¿Entiendo que la autenticación es la del POS?
- [ ] ¿Descarté los documentos viejos de la carpeta?

---

## RESUMEN

**Si alguien te pregunta:**

> "¿Qué estamos construyendo?"

**Responde:**

"Una nueva feature del POS que permite a los usuarios analizar el ROI de sus anuncios de Meta. Se integra en el menú principal del POS, usa los productos y ventas reales de la BD del usuario, y calcula automáticamente si cada anuncio es rentable o no. Se lanza cuando esté listo, y viene incluido con el plan que ya tienen."

---

## TIMELINE (Actualizado)

```
Lunes:       Dev team empieza (8 tareas, Sprint 1)
Semana 2:    Sprint 1 completado
Semana 3-4:  Alpha testing interno (10-15 usuarios del POS)
Semana 5-6:  Soft launch (todos los usuarios del POS)
             (no "soft launch de SaaS", sino "release de feature")
```

---

**Preguntas finales antes de empezar?** 🚀

Contacta a product@ o tech-lead@
