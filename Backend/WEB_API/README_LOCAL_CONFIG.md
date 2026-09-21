# 🔐 Configuración Local - Credenciales Sensibles

## ¿Por qué appsettings.local.json?

El archivo `appsettings.local.json` es **ignorado por Git** para evitar exponer credenciales de base de datos en el repositorio público.

## 📍 Ubicación del Archivo

```
Backend/WEB_API/appsettings.local.json
```

## 🔧 Cómo Usarlo

### 1. Crear el Archivo Local
Si no existe `appsettings.local.json`, el Backend usará `appsettings.Development.json` (localhost)

### 2. Agregar Credenciales de Producción
Copia el contenido que contiene las credenciales reales:

```json
{
  "TenantOptions": {
    "DefaultConnection": "Host=<tu-host>;Port=<tu-puerto>;Database=<tu-db>;User Id=<tu-usuario>;Password=<tu-contraseña>;Search Path=<tu-schema>;Maximum Pool Size=30"
  },
  // ... resto de configuración
}
```

**Nota:** Reemplaza los placeholders `<tu-*>` con las credenciales reales.

### 3. El Backend Cargará Automáticamente
.NET cargará `appsettings.local.json` después de `appsettings.Development.json`, sobrescribiendo la configuración.

## ⚠️ Importante

- **NUNCA** hagas commit de `appsettings.local.json` a GitHub
- El archivo está en `.gitignore` - Git lo ignorará automáticamente
- Solo comparte credenciales de forma segura (email, 1password, etc.)
- Cambia credenciales si se exponen accidentalmente

## 🔄 Orden de Carga (.NET)

```
1. appsettings.json (default)
2. appsettings.Development.json (dev overrides)
3. appsettings.local.json (local overrides - NUNCA en Git)
4. Variables de entorno
```

## ✅ Verificar Que Está Funcionando

```bash
# Si ves esta cadena de conexión → está usando Development (localhost)
Server=localhost;Port=3433;

# Si ves esta cadena de conexión → está usando Production (local config)
Host=38.242.252.183;Port=5432;
```

## 📋 Archivos de Configuración

| Archivo | Git | Propósito | Contiene |
|---------|-----|----------|----------|
| `appsettings.json` | ✅ Sí | Config por defecto | Config genérica |
| `appsettings.Development.json` | ✅ Sí | Config desarrollo local | localhost, claves de test |
| `appsettings.local.json` | ❌ No | Config específica tu máquina | Credenciales reales |

---

**Resumen:** 
- Usa `appsettings.local.json` para conectar a producción
- Git automáticamente lo ignora (está en .gitignore)
- No expondrá credenciales al repositorio
