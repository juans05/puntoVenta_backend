using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.IRepository;
using Domain.DTO;
using Domain.Entities;
using Domain.Entities.Identity;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;
    private readonly UserManager<User> _userManager;



    public TenantRepository(SpaContext dbContext, IMapper mapper, UserManager<User> userManager)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        _userManager = userManager;
    }

    public async Task<(ServiceStatus, object?, string)> CreateEmpresa(CreateEmpresaPayload payload)
    {
        try
        {
            //TODO: HACER CON TRANSACCION
            var entity = mapper.Map<Empresa>(payload);

            var empresaTenant = new EmpresaTenant
            {
                Empresa = entity,
                TenantId = payload.TenantId,
                Estado = true,
                FechaCreacion = DateTime.UtcNow.AddHours(-5)
            };

            await dbContext.EmpresaTenant.AddAsync(empresaTenant);

            await dbContext.SaveChangesAsync();



            var empresa = await dbContext.EmpresaTenant
                                         .Include(x => x.Empresa).ThenInclude(x => x.Ubigeo)
                                         .Include(y => y.Tenant)
                                         .FirstOrDefaultAsync(x => x.EmpresaId == entity.Id);

            var mapeo = mapper.Map<EmpresaDto>(empresa.Empresa);
            var userAdmin = empresa.Tenant.TenantKey;
            var userTenantName = empresa.Tenant.Name;


            //await CreateUserAdmin(userAdmin, payload.TenantId);

            //Crear Usuario
            var userResult = await CreateUserAdmin(userAdmin, payload.TenantId);

            //Asociar Usuarios a los modulos
            await AsociarModuleUser(userResult.Id, userTenantName);

            #region agregar funcionalidad para asociar categorias y productos al tenant, según el rubro

            #endregion

            return (ServiceStatus.Ok, mapeo, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error crear Tenant -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
    public async Task<(ServiceStatus, string)> AddEmpresaTenant(AddEmpresaPayload payload)
    {
        try
        {
            //TODO: HACER CON TRANSACCION

            var cantidadSucursales = await dbContext.Tenant.CountAsync(x => x.TenantKey == payload.tenantNombre);

            var newTenantName = payload.tenantNombre.ToUpper() + (cantidadSucursales + 1);

            var newTenant = new Tenant
            {
                Identificador = payload.identificador,
                TenantKey = payload.tenantNombre,
                Name = newTenantName,
            };

            await dbContext.Tenant.AddAsync(newTenant);


            var empresaTenant = new EmpresaTenant
            {
                EmpresaId = payload.idEmpresa,
                Tenant = newTenant,
                Estado = true,
                FechaCreacion = DateTime.UtcNow.AddHours(-5)
            };

            await dbContext.EmpresaTenant.AddAsync(empresaTenant);

            await dbContext.SaveChangesAsync();


            //Crear Usuario
            var userResult = await CreateUserAdmin(newTenantName, payload.identificador);

            //Asociar Usuarios a los modulos
            await AsociarModuleUser(userResult.Id, newTenantName);


            return (ServiceStatus.Ok, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, $"Error crear Tenant -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private async Task<User> CreateUserAdmin(string username, int identificador)
    {
        var newUser = new User
        {
            FirstName = username,
            LastName = username,
            FechaCreacion = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss"),
            UserName = username,
            TenantId = identificador,
            Estado = true,
            Email = "",
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(newUser, "123456");

        if (!result.Succeeded)
            throw new Exception($"No se pudo crear el usuario admin \"{username}\": {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return newUser;
    }

    private async Task AsociarModuleUser(string userId, string Tenant)
    {
        string dateString = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss");
        DateTime FechaRegistro = DateTime.ParseExact(dateString, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        // Todos los submódulos existentes en el catálogo, no una lista fija: así el admin
        // de una empresa nueva arranca con acceso completo sin depender de mantener este código.
        var submoduleIds = await dbContext.AspNetSubModule.AsNoTracking().Select(s => s.Identificador).ToListAsync();

        var newUserSubModule = submoduleIds.Select(submoduleId => new AspNetUserSubModule
        {
            UserId = userId,
            SubmoduleId = submoduleId,
            UsuarioCreacion = "ADMIN",
            FechaCreacion = FechaRegistro,
            Estado = true,
            TenantId = Tenant
        }).ToList();

        await dbContext.AspNetUserSubModule.AddRangeAsync(newUserSubModule);
        await dbContext.SaveChangesRegularAsync();
    }


    public async Task<(ServiceStatus, object?, string)> GetEmpresa(string tenantNombre)
    {
        try
        {
            var empresatenant = await dbContext.EmpresaTenant.Include(p => p.Empresa).ThenInclude(x => x.Ubigeo)
                                                              .AsNoTracking()
                                                              .Where(x => x.Tenant.TenantKey == tenantNombre)
                                                              .FirstOrDefaultAsync();

            if (empresatenant == null)
                return (ServiceStatus.FailedValidation, null, "No existe la empresa");

            var mapeoEmpresa = mapper.Map<EmpresaDto>(empresatenant.Empresa);
            mapeoEmpresa.TenantNombre = tenantNombre;

            return (ServiceStatus.Ok, mapeoEmpresa, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error get empresa -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, int, string)> CreateTenant(string nombre, int Rubro, ConfiguracionRentaPayload? configuracion)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return (ServiceStatus.FailedValidation, 0, "El nombre (tenant key) es obligatorio");

            nombre = nombre.Trim().ToLower();

            var yaExiste = await dbContext.Tenant.AsNoTracking().AnyAsync(x => x.TenantKey == nombre);

            if (yaExiste)
                return (ServiceStatus.FailedValidation, 0, $"Ya existe una empresa con el tenant key \"{nombre}\"");

            var identificador = (await dbContext.Tenant.AsNoTracking().Select(x => (int?)x.Identificador).MaxAsync() ?? 0) + 1;

            var tenant = new Tenant
            {
                Identificador = identificador,
                TenantKey = nombre,
                Name = nombre.ToUpper() + "1",
                RubroId = Rubro
            };

            await dbContext.Tenant.AddAsync(tenant);

            await dbContext.SaveChangesAsync();

            await CrearConfiguracionRentaTenant(tenant.Name, Rubro, configuracion);

            return (ServiceStatus.Ok, identificador, "Success");


        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, 0, $"Error crear Tenant -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private async Task CrearConfiguracionRentaTenant(string tenantName, int rubroId, ConfiguracionRentaPayload? configuracion)
    {
        var config = configuracion is not null
            ? configuracion
            : await ObtenerConfiguracionRubroPayload(rubroId) ?? ConfiguracionRentaFactory.ConfiguracionDefecto();

        var entidad = new ConfiguracionRenta
        {
            TenantId = tenantName,
            SucursalId = null,
            RubroId = null,
            Tipo = config.Tipo,
            TurnosJson = ConfiguracionRentaFactory.SerializarTurnos(config.Turnos),
            TarifasJson = ConfiguracionRentaFactory.SerializarTarifas(config.Tarifas),
            RecursosJson = ConfiguracionRentaFactory.SerializarRecursos(config.Recursos),
            Estado = true,
            FechaCreacion = DateTime.UtcNow.AddHours(-5),
        };

        await dbContext.ConfiguracionRenta.AddAsync(entidad);
        await dbContext.SaveChangesRegularAsync();

        await CrearRecursosTenant(tenantName, config.Recursos);

        // Si el rubro aún no tiene plantilla maestra, la primera empresa registrada la define
        var existePlantilla = await dbContext.ConfiguracionRenta.IgnoreQueryFilters()
            .AnyAsync(c => c.RubroId == rubroId);

        if (!existePlantilla)
        {
            var plantilla = new ConfiguracionRenta
            {
                TenantId = tenantName,
                SucursalId = null,
                RubroId = rubroId,
                Tipo = config.Tipo,
                TurnosJson = ConfiguracionRentaFactory.SerializarTurnos(config.Turnos),
                TarifasJson = ConfiguracionRentaFactory.SerializarTarifas(config.Tarifas),
                RecursosJson = ConfiguracionRentaFactory.SerializarRecursos(config.Recursos),
                Estado = true,
                FechaCreacion = DateTime.UtcNow.AddHours(-5),
            };

            await dbContext.ConfiguracionRenta.AddAsync(plantilla);
            await dbContext.SaveChangesRegularAsync();
        }
    }

    private async Task CrearRecursosTenant(string tenantName, List<RecursoConfigPayload>? recursos)
    {
        if (recursos is null || recursos.Count == 0)
            return;

        var existentes = await dbContext.Recurso.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantName)
            .Select(r => r.Descripcion)
            .ToListAsync();

        var nuevos = recursos
            .Where(r => !existentes.Contains(r.Descripcion))
            .Select(r => new Recurso
            {
                TenantId = tenantName,
                SucursalId = null,
                Descripcion = r.Descripcion,
                Zona = r.Zona,
                Tipo = r.Tipo,
                Estado = true,
                FechaCreacion = DateTime.UtcNow.AddHours(-5),
            })
            .ToList();

        if (nuevos.Count == 0)
            return;

        await dbContext.Recurso.AddRangeAsync(nuevos);
        await dbContext.SaveChangesRegularAsync();
    }

    private async Task<ConfiguracionRentaPayload?> ObtenerConfiguracionRubroPayload(int rubroId)
    {
        var plantilla = await dbContext.ConfiguracionRenta.IgnoreQueryFilters()
            .Where(c => c.RubroId == rubroId)
            .OrderByDescending(c => c.FechaCreacion)
            .FirstOrDefaultAsync();

        return plantilla is null ? null : ConfiguracionRentaFactory.ToPayload(plantilla);
    }

    public async Task<(ServiceStatus, object?, string)> GetConfiguracionRubro(int rubroId)
    {
        try
        {
            var config = await ObtenerConfiguracionRubroPayload(rubroId) ?? ConfiguracionRentaFactory.ConfiguracionDefecto();

            return (ServiceStatus.Ok, ConfiguracionRentaFactory.ConfiguracionToDto(config), "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener configuración del rubro -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> SaveConfiguracionRubro(int rubroId, ConfiguracionRentaPayload payload)
    {
        try
        {
            if (payload.Turnos is null || payload.Tarifas is null || payload.Recursos is null)
                return (ServiceStatus.FailedValidation, null, "La configuración debe incluir turnos, tarifas y recursos");

            var plantilla = await dbContext.ConfiguracionRenta.IgnoreQueryFilters()
                .Where(c => c.RubroId == rubroId)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefaultAsync();

            if (plantilla is null)
            {
                plantilla = new ConfiguracionRenta
                {
                    TenantId = dbContext.CurrentTenantName,
                    SucursalId = null,
                    RubroId = rubroId,
                    Estado = true,
                    FechaCreacion = DateTime.UtcNow.AddHours(-5),
                };

                await dbContext.ConfiguracionRenta.AddAsync(plantilla);
            }

            plantilla.Tipo = payload.Tipo;
            plantilla.TurnosJson = ConfiguracionRentaFactory.SerializarTurnos(payload.Turnos);
            plantilla.TarifasJson = ConfiguracionRentaFactory.SerializarTarifas(payload.Tarifas);
            plantilla.RecursosJson = ConfiguracionRentaFactory.SerializarRecursos(payload.Recursos);

            await dbContext.SaveChangesRegularAsync();

            return (ServiceStatus.Ok, ConfiguracionRentaFactory.ConfiguracionToDto(plantilla), "Plantilla del rubro guardada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al guardar configuración del rubro -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }


    public async Task<(ServiceStatus, object?, string)> GetRecursos(string tenant)
    {
        try
        {
            var res = await dbContext.EmpresaTenant.AsNoTracking()
                                                   .Where(p => p.Tenant.TenantKey == tenant.ToUpper())
                                                   .Select(x => x.Empresa)
                                                   .ToListAsync();


            if (res is null || res.Count == 0)
                return (ServiceStatus.NotFound, null, $"No se encontraron recursos del tenant => {tenant}");

            var resourses = res.Select(p => new
            {
                imagenPortada = p.ImagenPortada,
                gifCarga = p.GifCarga,
                logoSidebar = p.LogoSidebar,
                logo = p.Logo,
            }).FirstOrDefault();

            // Módulos del rubro: menú filtrado por RubroModulo (feature flags multi-rubro).
            var modulosRubro = await dbContext.Rubro.AsNoTracking()
                                                    .Where(r => r.Tenants.Any(t => t.TenantKey == tenant.ToUpper()))
                                                    .SelectMany(r => r.Moduulos)
                                                    .Where(m => m.Activo)
                                                    .Select(m => new
                                                    {
                                                        rubroId = m.RubroId,
                                                        codigoModulo = m.CodigoModulo,
                                                        activo = m.Activo
                                                    })
                                                    .ToListAsync();

            return (ServiceStatus.Ok, new { branding = resourses, modulos = modulosRubro }, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error Recursos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, Empresa?, string)> UpdateTenant(UpdateTenantPayload payload)
    {
        try
        {

            var Tenant = await dbContext.Empresa.AsNoTracking()
                                                 .Include(x => x.Ubigeo)
                                              .FirstAsync(p => p.Id == payload.Id);

            var entity = mapper.Map(payload, Tenant);

            dbContext.Entry(entity).State = EntityState.Modified;

            await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, entity, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Tenant -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }



    public async Task<(ServiceStatus, List<Empresa>?, string)> GetTenants()
    {

        try
        {
            List<Empresa> listaEmpresas = new();

            var res = await dbContext.EmpresaTenant.Include(q => q.Empresa)
                                                   .ThenInclude(q => q.Ubigeo)
                                                   .AsNoTracking()
                                                   .GroupBy(p => p.TenantId)
                                                   .Select(x => new
                                                   {
                                                       tenant = x.Key,
                                                       empresas = x.Select(p => p.Empresa)
                                                   })
                                                   .ToListAsync();

            if (res.Count == 0)
                return (ServiceStatus.FailedValidation, null, "No se encontraron tenants");

            foreach (var item in res)
            {
                listaEmpresas.AddRange(item.empresas);
            }

            return (ServiceStatus.Ok, listaEmpresas, "Succeeded");

        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar Productos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> GetAllTenants()
    {
        try
        {
            var res = await dbContext.Tenant.AsNoTracking()
                                            .GroupBy(p => p.TenantKey)
                                            .Select(x => x.Key)
                                            .ToListAsync();

            if (res.Count == 0)
                return (ServiceStatus.FailedValidation, null, "No se encontraron tenants");

            return (ServiceStatus.Ok, res, "Succeeded");

        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar Tenants -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> GetTenantsResumen()
    {
        try
        {
            var res = await dbContext.Tenant.AsNoTracking()
                                            .Include(t => t.Rubro)
                                            .Select(t => new TenantResumenDto
                                            {
                                                Identificador = t.Identificador,
                                                Name = t.Name,
                                                TenantKey = t.TenantKey,
                                                Activo = t.Activo,
                                                RubroId = t.RubroId,
                                                RubroNombre = t.Rubro.Nombre,
                                                NombreComercial = t.EmpresaTenants.Select(et => et.Empresa.NombreComercial).FirstOrDefault(),
                                                Ruc = t.EmpresaTenants.Select(et => et.Empresa.Ruc).FirstOrDefault(),
                                            })
                                            .ToListAsync();

            return (ServiceStatus.Ok, res, "Succeeded");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar Tenants -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> ReasignarModulos(int identificador)
    {
        try
        {
            var tenant = await dbContext.Tenant.AsNoTracking().FirstOrDefaultAsync(t => t.Identificador == identificador);

            if (tenant is null)
                return (ServiceStatus.NotFound, "No existe el tenant");

            var usuarios = await dbContext.Users.Where(u => u.TenantId == identificador).ToListAsync();

            foreach (var usuario in usuarios)
            {
                var existentes = dbContext.AspNetUserSubModule.IgnoreQueryFilters().Where(x => x.UserId == usuario.Id);
                dbContext.AspNetUserSubModule.RemoveRange(existentes);
                await dbContext.SaveChangesRegularAsync();

                await AsociarModuleUser(usuario.Id, tenant.Name);
            }

            return (ServiceStatus.Ok, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al reasignar módulos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> SetTenantActivo(int identificador, bool activo)
    {
        try
        {
            var tenant = await dbContext.Tenant.FirstOrDefaultAsync(t => t.Identificador == identificador);

            if (tenant is null)
                return (ServiceStatus.NotFound, "No existe el tenant");

            tenant.Activo = activo;

            await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, "Succeeded");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al actualizar Tenant -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

}