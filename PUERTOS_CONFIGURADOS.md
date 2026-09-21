# ⚙️ Puertos Configurados para Desarrollo Local

## Puertos en Uso

| Servicio | Puerto | URL |
|----------|--------|-----|
| **Frontend (Vite)** | 3000 | http://localhost:3000 |
| **Backend (.NET)** | 3001 | http://localhost:3001/api |
| **PostgreSQL** | 5433 | localhost:5433 |
| **Swagger (API Docs)** | 3001 | http://localhost:3001/swagger |

## Archivos Configurados

### Backend
- `Backend/WEB_API/Properties/launchSettings.json` → Puerto 3001
- `Backend/WEB_API/appsettings.Development.json` → PostgreSQL en puerto 5433

### Frontend
- `Frontend/vite.config.ts` → Puerto 3000 (ya estaba)
- `Frontend/.env` → VITE_API_URL=http://localhost:3001/api

## Levantar los Servicios

### 1. PostgreSQL en Docker (Puerto 5433)
```bash
docker run -d --name postgres-puntoventa \
  -e POSTGRES_USER=ventigo \
  -e POSTGRES_PASSWORD=ventigo_dev_2024 \
  -e POSTGRES_DB=PuntoVenta \
  -p 5433:5432 \
  postgres:14
```

### 2. Backend .NET (Puerto 3001)
```bash
cd Backend
dotnet restore
dotnet ef database update -p Infrastructure -s WEB_API
dotnet run --project WEB_API -c Debug
```
✅ Escucha en: **http://localhost:3001/api**

### 3. Frontend React (Puerto 3000)
```bash
cd Frontend
npm install
npm run dev
```
✅ Abre en: **http://localhost:3000**

## Verificar Servicios

```bash
# Frontend
curl http://localhost:3000

# Backend
curl http://localhost:3001/api

# API Swagger
curl http://localhost:3001/swagger

# PostgreSQL
psql -h localhost -p 5433 -U ventigo -d PuntoVenta
```

## Si Hay Conflicto de Puertos

Si algún puerto está en uso:

### Cambiar Puerto Frontend
Edita `Frontend/vite.config.ts`:
```typescript
server: {
  port: 4000  // Cambia a 4000 o el que necesites
}
```

### Cambiar Puerto Backend
Edita `Backend/WEB_API/Properties/launchSettings.json`:
```json
"applicationUrl": "http://localhost:8080"
```
Y luego actualiza `Frontend/.env`:
```
VITE_API_URL=http://localhost:8080/api
```

### Cambiar Puerto PostgreSQL
Edita `Backend/WEB_API/appsettings.Development.json`:
```json
"DefaultConnection": "Server=localhost;Port=5434;Database=..."
```
Y en Docker:
```bash
-p 5434:5432  # Cambio de puerto host:container
```

## Notas

- Los puertos están configurados para **desarrollo local**
- Para producción, usar puerto 80 (HTTP) y 443 (HTTPS)
- El `.env` del Frontend no se commitea (está en .gitignore) para evitar exponer configuraciones sensibles
- Si cambias puertos, asegúrate de actualizar todos los archivos de configuración

---

**Última actualización:** Sept 20, 2026
