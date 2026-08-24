-- ============================================================================
-- SEED MÍNIMO DE USUARIO ADMIN PARA LOGIN
-- Crea: módulos, submódulos, usuario ADMIN (password 123456) y accesos.
-- Ejecutar en la BD `puntoVenta` (esquema `wuarike_db`).
-- ============================================================================
BEGIN;

-- 1) Módulos (catálogo global, sin tenant)
INSERT INTO wuarike_db."AspNetModule" ("Identificador", "Nombre", "UsuarioCreacion", "FechaCreacion", "Estado")
SELECT q.ident, q.nombre, 'ADMIN', now(), true
FROM (VALUES
  ('200', 'Productos'),
  ('300', 'Ventas del día'),
  ('400', 'Clientes'),
  ('700', 'Reporte cierre de caja'),
  ('800', 'Mi empresa'),
  ('900', 'Documentos Facturados')
) AS q(ident, nombre)
WHERE NOT EXISTS (SELECT 1 FROM wuarike_db."AspNetModule" m WHERE m."Identificador" = q.ident);

-- 2) Submódulos (catálogo global, sin tenant)
INSERT INTO wuarike_db."AspNetSubModule" ("Identificador", "Nombre", "ModuloId", "UsuarioCreacion", "FechaCreacion", "Estado")
SELECT q.ident, q.nombre, q.modulo, 'ADMIN', now(), true
FROM (VALUES
  ('201', 'Productos', '200'),
  ('301', 'Ventas del día', '300'),
  ('401', 'Clientes', '400'),
  ('701', 'Reporte cierre de caja', '700'),
  ('801', 'Mi empresa', '800'),
  ('901', 'Documentos Facturados', '900')
) AS q(ident, nombre, modulo)
WHERE NOT EXISTS (SELECT 1 FROM wuarike_db."AspNetSubModule" s WHERE s."Identificador" = q.ident);

-- 3) Usuario ADMIN (password: 123456) — hash PBKDF2 v3 compatible con ASP.NET Core Identity 6.0.8
INSERT INTO wuarike_db."AspNetUsers"
  ("Id", "FirstName", "LastName", "FechaCreacion", "Estado", "TenantId", "UserName",
   "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash",
   "SecurityStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount", "SucursalId")
SELECT '00000000-0000-0000-0000-000000000001', 'Admin', 'Admin', now()::text, true, t."Identificador", 'ADMIN',
   'ADMIN', '', '', true,
   'AQAAAAEAACcQAAAAEBdiEDHFFBA9fpvYx2oh8b35rEacstmCNqrLkVkPISHY+NMNFWNrHHIoOWowGIc17g==',
   '00000000-0000-0000-0000-000000000001', true, false, false, 0, s."Id"
FROM wuarike_db."Tenant" t
LEFT JOIN wuarike_db."Sucursal" s ON s."TenantId" = t."Name"
WHERE t."Name" = 'SPASOLIS1'
  AND NOT EXISTS (SELECT 1 FROM wuarike_db."AspNetUsers" u WHERE u."UserName" = 'ADMIN');

-- 4) Asociar ADMIN a todos los submódulos (acceso total)
INSERT INTO wuarike_db."AspNetUserSubModule" ("UserId", "SubmoduleId", "UsuarioCreacion", "FechaCreacion", "Estado", "TenantId")
SELECT u."Id", sm."Identificador", 'ADMIN', now(), true, t."Name"
FROM wuarike_db."AspNetUsers" u
JOIN wuarike_db."Tenant" t ON t."Name" = 'SPASOLIS1'
JOIN wuarike_db."AspNetSubModule" sm ON true
WHERE u."UserName" = 'ADMIN'
  AND t."Name" = 'SPASOLIS1'
  AND NOT EXISTS (SELECT 1 FROM wuarike_db."AspNetUserSubModule" usm
                  WHERE usm."UserId" = u."Id" AND usm."SubmoduleId" = sm."Identificador");

COMMIT;

-- Verificación:
-- SELECT "UserName", "TenantId" FROM wuarike_db."AspNetUsers";
-- SELECT count(*) FROM wuarike_db."AspNetUserSubModule";