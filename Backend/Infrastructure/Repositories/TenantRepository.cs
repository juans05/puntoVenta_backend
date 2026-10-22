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

        // El admin fundador es el dueño de la cuenta: puede crear mas empresas y dar acceso a otros usuarios.
        await dbContext.UsuarioEmpresa.AddAsync(new UsuarioEmpresa
        {
            UserId = newUser.Id,
            TenantId = identificador,
            EsPropietario = true,
            Estado = true,
            FechaCreacion = DateTime.UtcNow.AddHours(-5)
        });
        await dbContext.SaveChangesRegularAsync();

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


    // ---------------------------------------------------------------- Mis empresas (dueño de la cuenta)

    private Task<bool> EsPropietario(string userId)
        => dbContext.UsuarioEmpresa.AnyAsync(x => x.UserId == userId && x.EsPropietario && x.Estado);

    // Empresas de las que el usuario es dueño, con cuantos usuarios tiene cada una. El cobro es por
    // usuario: TotalUsuarios son los usuarios distintos con acceso a alguna de sus empresas.
    public async Task<(ServiceStatus, object?, string)> ListarMisEmpresas(string userId)
    {
        var propias = await dbContext.UsuarioEmpresa.AsNoTracking()
            .Where(x => x.UserId == userId && x.EsPropietario && x.Estado)
            .Select(x => x.TenantId)
            .ToListAsync();

        var accesos = await dbContext.UsuarioEmpresa.AsNoTracking()
            .Where(x => propias.Contains(x.TenantId) && x.Estado)
            .Select(x => new { x.TenantId, x.UserId })
            .ToListAsync();

        var empresas = await dbContext.Tenant.AsNoTracking()
            .Where(t => propias.Contains(t.Identificador))
            .OrderBy(t => t.Identificador)
            .Select(t => new
            {
                tenantId = t.Identificador,
                empresa = t.EmpresaTenants.Select(et => et.Empresa).FirstOrDefault(),
                activo = t.Activo,
                planCuentasEnBlanco = t.PlanCuentasEnBlanco
            })
            .ToListAsync();

        return (ServiceStatus.Ok, new
        {
            totalUsuarios = accesos.Select(a => a.UserId).Distinct().Count(),
            empresas = empresas.Select(e => new
            {
                e.tenantId,
                nombre = e.empresa?.NombreComercial ?? e.empresa?.RazonSocial ?? "",
                razonSocial = e.empresa?.RazonSocial,
                ruc = e.empresa?.Ruc,
                e.activo,
                usuarios = accesos.Count(a => a.TenantId == e.tenantId)
            })
        }, "Success");
    }

    // El dueño crea otra empresa: tenant nuevo con sus catalogos, plan de cuentas segun la opcion
    // elegida (por defecto / en blanco / copiado de otra de sus empresas) y acceso completo para el.
    public async Task<(ServiceStatus, object?, string)> CrearEmpresaPropietario(string userId, CrearEmpresaPropietarioPayload payload)
    {
        if (!await EsPropietario(userId))
            return (ServiceStatus.FailedValidation, null, "Solo el dueño de la cuenta puede crear empresas");

        if (string.IsNullOrWhiteSpace(payload.RazonSocial))
            return (ServiceStatus.FailedValidation, null, "La razón social es obligatoria");

        var opcion = (payload.PlanCuentas ?? OpcionPlanCuentas.Defecto).ToUpper();
        if (opcion != OpcionPlanCuentas.Defecto && opcion != OpcionPlanCuentas.Blanco && opcion != OpcionPlanCuentas.Copiar)
            return (ServiceStatus.FailedValidation, null, "Opción de plan de cuentas inválida");

        Tenant? origenPlan = null;
        if (opcion == OpcionPlanCuentas.Copiar)
        {
            // Solo se copia de una empresa a la que el usuario tiene acceso.
            var tieneAcceso = payload.CopiarDeTenantId.HasValue && await dbContext.UsuarioEmpresa
                .AnyAsync(x => x.UserId == userId && x.TenantId == payload.CopiarDeTenantId && x.Estado);
            if (!tieneAcceso)
                return (ServiceStatus.FailedValidation, null, "Elige una de tus empresas para copiar el plan de cuentas");
            origenPlan = await dbContext.Tenant.AsNoTracking().FirstAsync(t => t.Identificador == payload.CopiarDeTenantId);
        }

        var actual = await dbContext.Tenant.AsNoTracking().FirstOrDefaultAsync(t => t.Name == dbContext.CurrentTenantName);
        var rubroId = payload.RubroId ?? actual?.RubroId ?? 0;
        if (rubroId == 0)
            return (ServiceStatus.FailedValidation, null, "Elige el rubro de la empresa");

        await using var transaccion = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var tenant = await CrearTenantEmpresa(payload, rubroId, actual?.PaisId, actual?.MonedaId,
                opcion == OpcionPlanCuentas.Blanco, origenPlan, _ => Task.FromResult(userId));

            await transaccion.CommitAsync();

            return (ServiceStatus.Ok, new { tenantId = tenant.Identificador, nombre = payload.NombreComercial ?? payload.RazonSocial }, "Empresa creada");
        }
        catch (Exception ex)
        {
            await transaccion.RollbackAsync();
            return (ServiceStatus.FailedValidation, null, $"Error al crear la empresa -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Registro publico: una persona crea su cuenta y su primera empresa, y queda como dueño.
    public async Task<(ServiceStatus, object?, string)> Registrar(RegistroPayload p)
    {
        if (string.IsNullOrWhiteSpace(p.Nombres) || string.IsNullOrWhiteSpace(p.Usuario) || string.IsNullOrWhiteSpace(p.Email))
            return (ServiceStatus.FailedValidation, null, "Completa tus nombres, usuario y correo");
        if (string.IsNullOrEmpty(p.Password) || p.Password.Length < 6)
            return (ServiceStatus.FailedValidation, null, "La contraseña debe tener al menos 6 caracteres");
        if (string.IsNullOrWhiteSpace(p.RazonSocial))
            return (ServiceStatus.FailedValidation, null, "Ingresa la razón social de tu empresa");
        if (!await dbContext.Rubro.AnyAsync(r => r.Id == p.RubroId))
            return (ServiceStatus.FailedValidation, null, "Elige el rubro de tu empresa");

        var usuario = p.Usuario.Trim().ToUpper();
        if (await _userManager.FindByNameAsync(usuario) is not null)
            return (ServiceStatus.FailedValidation, null, "Ese usuario ya existe, elige otro");
        if (await _userManager.FindByEmailAsync(p.Email.Trim()) is not null)
            return (ServiceStatus.FailedValidation, null, "Ese correo ya está registrado");

        await using var transaccion = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var empresa = new CrearEmpresaPropietarioPayload
            {
                RazonSocial = p.RazonSocial,
                NombreComercial = p.NombreComercial,
                Ruc = p.Ruc
            };
            await CrearTenantEmpresa(empresa, p.RubroId, null, null, false, null, async tenant =>
            {
                var user = new User
                {
                    FirstName = p.Nombres.Trim(),
                    LastName = p.Apellidos?.Trim() ?? "",
                    UserName = usuario,
                    Email = p.Email.Trim(),
                    PhoneNumber = p.Celular?.Trim(),
                    Celular = p.Celular?.Trim(),
                    TenantId = tenant.Identificador,
                    FechaCreacion = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss"),
                    Estado = true,
                    EmailConfirmed = true
                };
                var resultado = await _userManager.CreateAsync(user, p.Password);
                if (!resultado.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
                return user.Id;
            });

            await transaccion.CommitAsync();
            return (ServiceStatus.Ok, new { usuario }, "Cuenta creada");
        }
        catch (Exception ex)
        {
            await transaccion.RollbackAsync();
            return (ServiceStatus.FailedValidation, null, $"No se pudo crear la cuenta -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Crea la empresa completa: tenant, datos de la empresa, catalogos, plan de cuentas (por defecto,
    // en blanco o copiado) y el dueño con acceso total. `obtenerDueño` recibe el tenant ya guardado y
    // devuelve el Id del dueño (en el registro crea al usuario ahi, porque el usuario necesita el tenant).
    // No abre transaccion: la abre quien llama.
    private async Task<Tenant> CrearTenantEmpresa(CrearEmpresaPropietarioPayload payload, int rubroId, int? paisId, int? monedaId,
        bool planEnBlanco, Tenant? origenPlan, Func<Tenant, Task<string>> obtenerDueño)
    {
        var tenantKey = await GenerarTenantKey(payload.Ruc, payload.NombreComercial ?? payload.RazonSocial);
        var tenant = new Tenant
        {
            Identificador = (await dbContext.Tenant.Select(x => (int?)x.Identificador).MaxAsync() ?? 0) + 1,
            TenantKey = tenantKey,
            Name = tenantKey.ToUpper() + "1",
            RubroId = rubroId,
            PaisId = paisId,
            MonedaId = monedaId,
            Activo = true,
            PlanCuentasEnBlanco = planEnBlanco
        };
        await dbContext.Tenant.AddAsync(tenant);

        await dbContext.EmpresaTenant.AddAsync(new EmpresaTenant
        {
            Empresa = new Empresa
            {
                RazonSocial = payload.RazonSocial.Trim(),
                NombreComercial = payload.NombreComercial?.Trim(),
                Ruc = payload.Ruc?.Trim(),
                Direccion = payload.Direccion?.Trim()
            },
            Tenant = tenant,
            Estado = true,
            FechaCreacion = DateTime.UtcNow.AddHours(-5)
        });
        await dbContext.SaveChangesRegularAsync();

        var userId = await obtenerDueño(tenant);
        await dbContext.UsuarioEmpresa.AddAsync(new UsuarioEmpresa
        {
            UserId = userId,
            TenantId = tenant.Identificador,
            EsPropietario = true,
            Estado = true,
            FechaCreacion = DateTime.UtcNow.AddHours(-5)
        });
        await dbContext.SaveChangesRegularAsync();

        await CrearConfiguracionRentaTenant(tenant.Name, rubroId, null);

        // Se copia antes del sembrado: el sembrado solo pone el PCGE si la empresa no tiene cuentas.
        if (origenPlan is not null)
            await CopiarPlanDeCuentas(origenPlan.Name, tenant.Name);

        dbContext.ChangeTracker.Clear();
        await PuntoVentaDbContextData.SeedCatalogoTenant(dbContext, tenant.Name);

        // El sembrado deja la identidad fiscal vacia; se completa con los datos ingresados.
        var fiscal = await dbContext.ConfiguracionFiscal.IgnoreQueryFilters().Where(x => x.TenantId == tenant.Name).ToListAsync();
        foreach (var f in fiscal)
        {
            f.Ruc = payload.Ruc?.Trim();
            f.RazonSocial = payload.RazonSocial.Trim();
            f.NombreComercial = payload.NombreComercial?.Trim() ?? payload.RazonSocial.Trim();
            f.Direccion = payload.Direccion?.Trim();
        }
        await dbContext.SaveChangesRegularAsync();

        await AsociarModuleUser(userId, tenant.Name);
        return tenant;
    }

    // TenantKey unico en minusculas: el RUC si viene, si no el nombre sin espacios ni simbolos.
    private async Task<string> GenerarTenantKey(string? ruc, string nombre)
    {
        var limpio = System.Text.RegularExpressions.Regex.Replace(
            (string.IsNullOrWhiteSpace(ruc) ? nombre : ruc).Trim().ToLower(), "[^a-z0-9]", "");
        var baseKey = string.IsNullOrEmpty(limpio) ? "empresa" : limpio[..Math.Min(limpio.Length, 30)];
        var key = baseKey;
        for (var i = 2; await dbContext.Tenant.AnyAsync(t => t.TenantKey == key || t.Name == key.ToUpper() + "1"); i++)
            key = baseKey + i;
        return key;
    }

    // Copia el plan de cuentas completo de una empresa a otra, rehaciendo las referencias entre cuentas
    // (padre, cargo/abono de destino y cierre) con los Id nuevos.
    private async Task CopiarPlanDeCuentas(string origen, string destino)
    {
        var cuentas = await dbContext.CuentaContable.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.TenantId == origen)
            .OrderBy(c => c.Id)
            .ToListAsync();
        if (cuentas.Count == 0) return;

        var refs = cuentas.Select(c => new[]
        {
            c.CuentaPadreId, c.CuentaCargo1Id, c.CuentaAbono1Id, c.CuentaCargo2Id,
            c.CuentaAbono2Id, c.CuentaCargo3Id, c.CuentaAbono3Id, c.CuentaCierreId
        }).ToList();
        var idsOrigen = cuentas.Select(c => c.Id).ToList();

        foreach (var c in cuentas)
        {
            c.Id = 0;
            c.TenantId = destino;
            c.CuentaPadreId = c.CuentaCargo1Id = c.CuentaAbono1Id = c.CuentaCargo2Id = null;
            c.CuentaAbono2Id = c.CuentaCargo3Id = c.CuentaAbono3Id = c.CuentaCierreId = null;
        }
        await dbContext.CuentaContable.AddRangeAsync(cuentas);
        await dbContext.SaveChangesRegularAsync();

        var nuevoId = idsOrigen.Zip(cuentas, (viejo, c) => (viejo, c.Id)).ToDictionary(x => x.viejo, x => x.Id);
        int? Nuevo(int? viejo) => viejo.HasValue && nuevoId.TryGetValue(viejo.Value, out var n) ? n : null;

        for (var i = 0; i < cuentas.Count; i++)
        {
            var c = cuentas[i];
            var r = refs[i];
            c.CuentaPadreId = Nuevo(r[0]);
            c.CuentaCargo1Id = Nuevo(r[1]); c.CuentaAbono1Id = Nuevo(r[2]);
            c.CuentaCargo2Id = Nuevo(r[3]); c.CuentaAbono2Id = Nuevo(r[4]);
            c.CuentaCargo3Id = Nuevo(r[5]); c.CuentaAbono3Id = Nuevo(r[6]);
            c.CuentaCierreId = Nuevo(r[7]);
        }
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

            // Mantiene la identidad fiscal (la que se usa para emitir comprobantes ante SUNAT,
            // via ObtenerConfiguracionFiscalPorTenant -- que busca solo por TenantId) sincronizada
            // con los datos del negocio que el usuario acaba de guardar aquí. Antes se buscaba por
            // EmpresaId, pero esa columna puede venir sin vincular en datos legacy (visto en
            // produccion: ConfiguracionFiscal.EmpresaId nulo) y ademas no tiene query filter global
            // por TenantId como la mayoria de entidades en SpaContext -- una Empresa compartida
            // entre varios Tenants (EmpresaTenants) podia terminar actualizando la de OTRO tenant
            // (FirstOrDefaultAsync sin ORDER BY no es deterministico). Buscar directamente por
            // TenantId (igual criterio que la lectura) evita ambos problemas y de paso repara el
            // vinculo a EmpresaId si nunca se habia seteado.
            var configFiscal = await dbContext.ConfiguracionFiscal
                .FirstOrDefaultAsync(c => c.TenantId == dbContext.CurrentTenantName && c.Activo && c.Estado);
            if (configFiscal != null)
            {
                var ubigeo = entity.UbigeoId != null
                    ? await dbContext.Ubigeo.FirstOrDefaultAsync(u => u.UbigeoId == entity.UbigeoId)
                    : null;

                configFiscal.EmpresaId = entity.Id;
                configFiscal.Ruc = entity.Ruc;
                configFiscal.RazonSocial = entity.RazonSocial;
                configFiscal.NombreComercial = entity.NombreComercial;
                configFiscal.Direccion = entity.Direccion;
                configFiscal.UbigeoId = entity.UbigeoId;
                configFiscal.Departamento = ubigeo?.Departamento;
                configFiscal.Provincia = ubigeo?.Provincia;
                configFiscal.Distrito = ubigeo?.Distrito;

                dbContext.Entry(configFiscal).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();
            }

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