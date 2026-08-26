using Domain.Entities;
using Domain.Entities.Identity;
using Domain.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class PuntoVentaDbContextData
    {
        public static async Task LoadDataAsync(
        SpaContext context,
        ILoggerFactory loggerFactory
       )
        {
            try
            {
                // Catálogos maestros globales (sin TenantId): Pais, Moneda, Impuesto, Rubro, RubroModulo.
                await SeedCatalogoGlobal(context);

                // Catálogos maestros que requieren un TenantId.
                // Se siembran para cada tenant activo (catálogos por tenant, no globales).
                var tenantNames = ObtenerTenantsActivos();

                foreach (var tenantName in tenantNames)
                {
                    await SeedCatalogoTenant(context, tenantName);
                }

                // Los JSON de seed traen "id" explícito para tener IDs estables entre entornos.
                // Insertar con Id explícito no avanza la secuencia identity de Postgres, así que
                // el próximo insert real (sin Id, ej. un usuario crea un método de pago nuevo)
                // puede pedir un Id ya usado por el seed y explotar con 23505 (duplicate key).
                // Se corre siempre (no solo la primera vez) porque es barata e idempotente.
                await ResincronizarSecuenciasAsync(context);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger<PuntoVentaDbContextData>();
                logger.LogError(ex.Message);
            }
        }

        private static readonly string[] TablasConIdDeSeedExplicito =
        {
            "Categoria", "Grupo", "Impuesto", "Metodopago", "Moneda", "Pais",
            "Producto", "Proveedor", "Rubro", "RubroModulo", "Seriecorrelativo",
            "TipoDocumento", "TipoDocumentoVenta"
        };

        private static async Task ResincronizarSecuenciasAsync(SpaContext context)
        {
            foreach (var tabla in TablasConIdDeSeedExplicito)
            {
                await context.Database.ExecuteSqlRawAsync(
                    $"SELECT setval(pg_get_serial_sequence('\"{tabla}\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"{tabla}\"), 1))");
            }
        }

        private static string[] ObtenerTenantsActivos()
        {
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .Build();

            var tenants = configuration.GetSection("TenantOptions:Tenants")
                                       .Get<List<Tenantx>>();

            var names = tenants?.Where(t => !string.IsNullOrEmpty(t.Name))
                                .Select(t => t.Name!.ToUpper())
                                .Distinct()
                                .ToArray();

            return names is { Length: > 0 } ? names : new[] { "SPASOLIS1" };
        }

        private static async Task SeedCatalogoGlobal(SpaContext context)
        {
            // Catálogos maestros globales compartidos entre tenants (sin query filter por TenantId).
            if (!context.Pais.Any())
            {
                var paisData = File.ReadAllText("../Infrastructure/Data/Default/pais.json");
                await context.Pais.AddRangeAsync(JsonConvert.DeserializeObject<List<Pais>>(paisData));
                await context.SaveChangesAsync();
            }

            if (!context.Moneda.Any())
            {
                var monedaData = File.ReadAllText("../Infrastructure/Data/Default/moneda.json");
                await context.Moneda.AddRangeAsync(JsonConvert.DeserializeObject<List<Moneda>>(monedaData));
                await context.SaveChangesAsync();
            }

            if (!context.Impuesto.Any())
            {
                var impuestoData = File.ReadAllText("../Infrastructure/Data/Default/impuesto.json");
                await context.Impuesto.AddRangeAsync(JsonConvert.DeserializeObject<List<Impuesto>>(impuestoData));
                await context.SaveChangesAsync();
            }

            if (!context.Rubro.Any())
            {
                var rubroData = File.ReadAllText("../Infrastructure/Data/Default/rubro.json");
                await context.Rubro.AddRangeAsync(JsonConvert.DeserializeObject<List<Rubro>>(rubroData));
                await context.SaveChangesAsync();
            }

            if (!context.RubroModulo.Any())
            {
                var rubroModuloData = File.ReadAllText("../Infrastructure/Data/Default/rubromodulo.json");
                await context.RubroModulo.AddRangeAsync(JsonConvert.DeserializeObject<List<RubroModulo>>(rubroModuloData));
                await context.SaveChangesAsync();
            }

            // Catálogo de ubigeos (INEI/RENIEC): departamentos, provincias y distritos del Perú.
            if (!context.Ubigeo.Any())
            {
                var ubigeoData = File.ReadAllText("../Infrastructure/Data/Default/ubigeo.json");
                await context.Ubigeo.AddRangeAsync(JsonConvert.DeserializeObject<List<Ubigeo>>(ubigeoData));
                await context.SaveChangesAsync();
            }

            // Catálogo de módulos/submódulos: chequeo por fila (no por tabla) porque el catálogo
            // crece con el tiempo (ej. se agregó "Compras") y ya existen filas de antes.
            var moduloData = File.ReadAllText("../Infrastructure/Data/Default/module.json");
            var modulos = JsonConvert.DeserializeObject<List<AspNetModule>>(moduloData);
            var modulosExistentes = await context.AspNetModule.Select(m => m.Identificador).ToListAsync();
            var modulosFaltantes = modulos.Where(m => !modulosExistentes.Contains(m.Identificador)).ToList();

            if (modulosFaltantes.Count > 0)
            {
                modulosFaltantes.ForEach(m => m.FechaCreacion = DateTime.UtcNow.AddHours(-5));
                await context.AspNetModule.AddRangeAsync(modulosFaltantes);
                await context.SaveChangesRegularAsync();
            }

            var subModuloData = File.ReadAllText("../Infrastructure/Data/Default/subModule.json");
            var subModulos = JsonConvert.DeserializeObject<List<AspNetSubModule>>(subModuloData);
            var subModulosExistentes = await context.AspNetSubModule.Select(s => s.Identificador).ToListAsync();
            var subModulosFaltantes = subModulos.Where(s => !subModulosExistentes.Contains(s.Identificador)).ToList();

            if (subModulosFaltantes.Count > 0)
            {
                subModulosFaltantes.ForEach(s => s.FechaCreacion = DateTime.UtcNow.AddHours(-5));
                await context.AspNetSubModule.AddRangeAsync(subModulosFaltantes);
                await context.SaveChangesRegularAsync();
            }
        }

        private static async Task SeedCatalogoTenant(SpaContext context, string tenantId)
        {
            // Los catálogos con FK se insertan antes que sus dependientes.
            // TipoDocumentoVenta se inserta primero porque Seriecorrelativo depende de él.
            if (!context.TipoDocumento.IgnoreQueryFilters().Any(x => x.TenantId == tenantId))
            {
                var tipoDocumentoData = File.ReadAllText("../Infrastructure/Data/Default/tipodocumento.json");
                var tipoDocumento = JsonConvert.DeserializeObject<List<TipoDocumento>>(tipoDocumentoData);
                tipoDocumento.ForEach(x => x.TenantId = tenantId);
                await context.TipoDocumento!.AddRangeAsync(tipoDocumento);
                await context.SaveChangesRegularAsync();
            }

            if (!context.TipoDocumentoVenta.IgnoreQueryFilters().Any(x => x.TenantId == tenantId))
            {
                var tipoDocumentoVentaData = File.ReadAllText("../Infrastructure/Data/Default/tipodocumentoventa.json");
                var tipoDocumentoVenta = JsonConvert.DeserializeObject<List<TipoDocumentoVenta>>(tipoDocumentoVentaData);
                tipoDocumentoVenta.ForEach(x => x.TenantId = tenantId);
                await context.TipoDocumentoVenta!.AddRangeAsync(tipoDocumentoVenta);
                await context.SaveChangesRegularAsync();
            }

            if (!context.Seriecorrelativo.IgnoreQueryFilters().Any(x => x.TenantId == tenantId))
            {
                var serieCorrelativoData = File.ReadAllText("../Infrastructure/Data/Default/seriecorrelativo.json");
                var serieCorrelativo = JsonConvert.DeserializeObject<List<Seriecorrelativo>>(serieCorrelativoData);

                // Asigna la sede principal del tenant (o null si aún no tiene sucursal) para aislar correlativos por sede.
                var sucursalId = context.Sucursal.IgnoreQueryFilters()
                                             .Where(x => x.TenantId == tenantId)
                                             .Select(x => (int?)x.Id)
                                             .FirstOrDefault();

                serieCorrelativo.ForEach(x =>
                {
                    x.TenantId = tenantId;
                    x.SucursalId = sucursalId;
                });
                await context.Seriecorrelativo.AddRangeAsync(serieCorrelativo);
                await context.SaveChangesRegularAsync();
            }

            if (!context.Metodopago.IgnoreQueryFilters().Any(x => x.TenantId == tenantId))
            {
                var metodoPagoData = File.ReadAllText("../Infrastructure/Data/Default/metodopago.json");
                var metodoPago = JsonConvert.DeserializeObject<List<Metodopago>>(metodoPagoData);
                metodoPago.ForEach(x => x.TenantId = tenantId);
                await context.Metodopago!.AddRangeAsync(metodoPago);
                await context.SaveChangesRegularAsync();
            }

            if (!context.ConfiguracionFiscal.IgnoreQueryFilters().Any(x => x.TenantId == tenantId))
            {
                var configuracionFiscalData = File.ReadAllText("../Infrastructure/Data/Default/configuracionfiscal.json");
                var configuracionFiscal = JsonConvert.DeserializeObject<List<ConfiguracionFiscal>>(configuracionFiscalData);
                configuracionFiscal.ForEach(x =>
                {
                    x.TenantId = tenantId;
                    if (tenantId == "SPASOLIS1" && string.IsNullOrEmpty(x.Ruc))
                    {
                        // solo seed de demostración; se rellenará vía UI/onboarding
                    }
                });
                await context.ConfiguracionFiscal!.AddRangeAsync(configuracionFiscal);
                await context.SaveChangesRegularAsync();
            }

            // Sede principal por tenant (la sucursal default que resuelve X-Sucursal).
            // Requiere los catálogos globales sembrados antes (Pais/Moneda/Rubro).
            if (!context.Sucursal.IgnoreQueryFilters().Any(x => x.TenantId == tenantId))
            {
                var sucursalData = File.ReadAllText("../Infrastructure/Data/Default/sucursal.json");
                var sucursales = JsonConvert.DeserializeObject<List<Sucursal>>(sucursalData);
                sucursales.ForEach(x => x.TenantId = tenantId);
                await context.Sucursal!.AddRangeAsync(sucursales);
                await context.SaveChangesRegularAsync();
            }
        }
    }
}