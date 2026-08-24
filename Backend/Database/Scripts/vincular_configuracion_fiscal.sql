-- ============================================================================
-- VINCULAR ConfiguracionFiscal: EmpresaId/PaisId, series reales y token desde BD
-- Ejecutar MANUALMENTE en la BD `puntoVenta` (esquema `wuarike_db`).
-- Ajusta el valor de :TOKEN según el adaptador (API Perú / SUNAT).
-- ============================================================================
BEGIN;

-- 1) Crear la Empresa de SPASOLIS1 (si aún no existe)
--    NOTA: UbigeoId se deja NULL porque el catálogo Ubigeo viene de fuente externa y
--    la tabla local puede no contener el código (evita FK errors). Si tu BD tiene el
--    ubigeo, descomenta la línea y baja el UPDATE 1.
INSERT INTO wuarike_db."Empresa" ("Ruc", "NombreComercial", "RazonSocial", "Direccion")
SELECT '10430936315', 'SOLIS SALON & SPA', 'SOLIS EGUIZABAL VENTURA', 'URB. MERCURIO CAL. CESAR VALLEJO 881'
WHERE NOT EXISTS (SELECT 1 FROM wuarike_db."Empresa" q WHERE q."Ruc" = '10430936315');

-- 2) Crear el Tenant SPASOLIS1 en la tabla Tenant (catálogo de tenants)
--    RubroId=1 (Spa), MonedaId=1 (PEN), PaisId=604 (Perú)
INSERT INTO wuarike_db."Tenant" ("Identificador", "Name", "TenantKey", "RubroId", "MonedaId", "PaisId")
SELECT COALESCE((SELECT MAX(q."Identificador") + 1 FROM wuarike_db."Tenant" q), 1), 'SPASOLIS1', 'spasolis', 1, 1, 604
WHERE NOT EXISTS (SELECT 1 FROM wuarike_db."Tenant" q WHERE q."Name" = 'SPASOLIS1');

-- 3) Puente Empresa-Tenant
--    NOTA: EmpresaTenant.TenantId es INT y referencia Tenant.Identificador (no el nombre).
INSERT INTO wuarike_db."EmpresaTenant" ("EmpresaId", "TenantId", "UsuarioCreacion", "FechaCreacion", "Estado")
SELECT e."Id", t."Identificador", 'Admin', now(), true
FROM wuarike_db."Empresa" e
JOIN wuarike_db."Tenant" t ON t."Name" = 'SPASOLIS1'
WHERE e."Ruc" = '10430936315'
  AND NOT EXISTS (SELECT 1 FROM wuarike_db."EmpresaTenant" q
                  WHERE q."EmpresaId" = e."Id" AND q."TenantId" = t."Identificador");

-- 4) Vincular EmpresaId + PaisId + series reales + token en ConfiguracionFiscal
--    (la tabla CF usa la columna `Pais` como código ISO; el PaisId vive en Tenant)
UPDATE wuarike_db."ConfiguracionFiscal" cf
SET "EmpresaId"       = (SELECT e."Id" FROM wuarike_db."Empresa" e WHERE e."Ruc" = '10430936315'),
    "Pais"            = 'PE',
    "SerieFactura"    = 'F001',
    "SerieBoleta"     = 'B001',
    "SerieNota"       = 'RC01',
    "CodigoAdaptador" = 'SUNAT_APIPERU',
    "Token"           = 'CAMBIAR-POR-TOKEN-REAL-DEL-ADAPTADOR',
    "Moneda"          = 'PEN',
    "PorcentajeImpuesto" = 18,
    "Activo"          = true
WHERE cf."TenantId" = 'SPASOLIS1';

-- 5) Asignar SucursalId a la sede principal en la configuración fiscal (sede única por ahora)
--    (relación vía Tenant.SucursalId; si la sucursal no aparece, el query filter usa sucursal null)

-- 6) CORREGIR SERIES a valores SUNAT reales (F001/B001) y aislarlas por sede
--    (antes se sembraron como BOL/FAC; el correlativo por serie nunca encontraba F001/B001)
UPDATE wuarike_db."Seriecorrelativo" sc
SET "Serie" = CASE sc."TipoDocumentoVentaId"
                  WHEN 1 THEN 'F001'   -- TipoDocumentoVentaId 1 -> SerieFactura (enum Factura=1)
                  WHEN 2 THEN 'B001'   -- TipoDocumentoVentaId 2 -> SerieBoleta (enum Boleta=2)
                  ELSE sc."Serie" END,
    "SucursalId" = (SELECT s."Id" FROM wuarike_db."Sucursal" s
                    WHERE s."TenantId" = sc."TenantId" ORDER BY s."Id" LIMIT 1)
WHERE sc."TenantId" = 'SPASOLIS1';

COMMIT;

-- Verificación:
-- SELECT * FROM wuarike_db."ConfiguracionFiscal";
-- SELECT * FROM wuarike_db."Empresa";
-- SELECT * FROM wuarike_db."Tenant";
-- SELECT * FROM wuarike_db."EmpresaTenant";
-- SELECT * FROM wuarike_db."Sucursal";