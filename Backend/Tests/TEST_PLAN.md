# Plan de Testing - PuntoVenta

## Checklist de Flujos a Testear

### 1. PRODUCTOS ✅
- [x] Crear producto con todos los campos
- [ ] Crear producto mínimo
- [ ] Actualizar producto
- [ ] Eliminar producto
- [ ] Listar productos
- [ ] Buscar producto por código
- [ ] Actualizar stock de producto
- [ ] Validar código único
- [ ] Validar precio > 0

### 2. CATEGORÍAS ✅
- [x] Crear categoría
- [x] Actualizar categoría
- [x] Listar categorías
- [ ] Eliminar categoría
- [ ] Validar nombre único

### 3. FACTURAS/COMPROBANTES ✅
- [x] Crear factura (Boleta)
- [x] Agregar línea de producto a factura
- [x] Calcular totales (subtotal, IGV, total)
- [x] Agregar pago a factura
- [ ] Crear factura (Factura)
- [ ] Emitir factura
- [ ] Anular factura
- [x] Cambiar estado de factura
- [ ] Listar facturas
- [ ] Filtrar facturas por fecha
- [ ] Validar número único de factura

### 4. COMPRAS
- [ ] Crear orden de compra
- [ ] Agregar productos a compra
- [ ] Calcular costo de compra
- [ ] Recibir compra
- [ ] Actualizar stock por compra
- [ ] Registrar costo unitario
- [ ] Anular compra
- [ ] Listar compras

### 5. CAJAS
- [ ] Abrir caja
- [ ] Cerrar caja
- [ ] Registrar movimiento de caja
- [ ] Validar saldo de caja
- [ ] Resumen de caja
- [ ] Arqueo de caja

### 6. INVENTARIO/STOCK
- [ ] Ajuste de stock (entrada)
- [ ] Ajuste de stock (salida)
- [ ] Validar stock mínimo
- [ ] Alertas de bajo stock
- [ ] Listado de inventario
- [ ] Movimientos de stock

### 7. REPORTES
- [ ] Reporte de ventas por día
- [ ] Reporte de ventas por período
- [ ] Reporte de productos más vendidos
- [ ] Reporte de clientes
- [ ] Reporte de caja
- [ ] Reporte de compras

### 8. USUARIOS/SEGURIDAD
- [ ] Crear usuario
- [ ] Autenticación de usuario
- [ ] Cambiar contraseña
- [ ] Permisos de usuario
- [ ] Auditoría de acciones

### 9. MÉTODOS DE PAGO
- [ ] Pago en efectivo
- [ ] Pago con tarjeta
- [ ] Pago parcial
- [ ] Múltiples pagos por factura

### 10. DATOS MAESTROS
- [ ] Monedas
- [ ] Unidades de medida
- [ ] Tipos de IGV
- [ ] Sucursales

## Estadísticas

**Total de flujos:** 10
**Total de tests requeridos:** 60+
**Tests completados:** 8 ✅

| Módulo | Estado | Tests | Pasando |
|--------|--------|-------|---------|
| Productos | ✅ | 1 | 1/1 |
| Comprobantes | ✅ | 4 | 4/4 |
| Categorías | ✅ | 3 | 3/3 |
| Compras | ⏳ | 8 | 0/8 |
| Cajas | ⏳ | 6 | 0/6 |
| Inventario | ⏳ | 8 | 0/8 |
| Reportes | ⏳ | 6 | 0/6 |
| Usuarios | ⏳ | 5 | 0/5 |
| Pagos | ⏳ | 4 | 0/4 |
| Datos Maestros | ⏳ | 7 | 0/7 |

**Progreso General:** 8/60 (13%)

## Próximos Pasos

Para expandir la cobertura de tests, se necesita:
1. Investigar las entidades exactas (Compra, CompraDetalle, AspNetUser, Auditoria, etc.)
2. Crear tests para Cajas (6 tests)
3. Crear tests para Inventario (8 tests)
4. Crear tests para Métodos de Pago (4 tests)
5. Crear tests para Datos Maestros (7 tests)
6. Crear tests para Reportes (6 tests)
7. Crear tests para Usuarios (5 tests)

## Convenciones de Testing

- Usar patrón AAA: Arrange, Act, Assert
- Nombres de test descriptivos
- Un concepto por test
- Reutilizar helpers de setup
- Limpiar datos después de cada test
