using Application.Interfaces.IRepository;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Infrastructure.Repositories;

public class RentaRepositorio : IRentaRepositorio
{
    private const string FormatoFecha = "dd/MM/yyyy";
    private const string FormatoFechaHora = "dd/MM/yyyy HH:mm:ss";

    private readonly SpaContext _context;

    public RentaRepositorio(SpaContext context)
    {
        _context = context;
    }

    private static DateTime Ahora() => DateTime.UtcNow.AddHours(-5);

    private static bool TryParseFecha(string fecha, out DateTime value) =>
        DateTime.TryParseExact(fecha, FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);

    private async Task<ConfiguracionRenta> EnsureConfiguracionAsync()
    {
        var configuracion = await _context.ConfiguracionRenta.FirstOrDefaultAsync();

        if (configuracion != null)
            return configuracion;

        var defecto = ConfiguracionRentaFactory.ConfiguracionDefecto();

        configuracion = new ConfiguracionRenta
        {
            SucursalId = _context.CurrentSucursalId,
            Tipo = defecto.Tipo,
            TurnosJson = ConfiguracionRentaFactory.SerializarTurnos(defecto.Turnos),
            TarifasJson = ConfiguracionRentaFactory.SerializarTarifas(defecto.Tarifas),
            RecursosJson = ConfiguracionRentaFactory.SerializarRecursos(defecto.Recursos),
        };

        await _context.ConfiguracionRenta.AddAsync(configuracion);
        await _context.SaveChangesAsync();

        return configuracion;
    }

    private async Task EnsureRecursosAsync(ConfiguracionRenta configuracion)
    {
        var recursos = ConfiguracionRentaFactory.DeserializarRecursos(configuracion.RecursosJson);

        var existentes = await _context.Recurso.AsNoTracking()
            .Select(r => r.Descripcion)
            .ToListAsync();

        var nuevos = recursos
            .Where(r => !existentes.Contains(r.Descripcion))
            .Select(r => new Recurso
            {
                SucursalId = configuracion.SucursalId,
                Descripcion = r.Descripcion,
                Zona = r.Zona,
                Tipo = r.Tipo,
            })
            .ToList();

        if (nuevos.Count == 0)
            return;

        await _context.Recurso.AddRangeAsync(nuevos);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureProductosAsync()
    {
        var configuracion = await EnsureConfiguracionAsync();
        await EnsureRecursosAsync(configuracion);
    }

    private IQueryable<Renta> QueryRentas()
    {
        return _context.Renta
            .Include(r => r.Recurso)
            .Include(r => r.Anfitriona)!
            .ThenInclude(a => a.Nacionalidad)
            .AsNoTracking();
    }

    private static object RentaToDto(Renta renta) => new
    {
        id = renta.Id,
        habitacion = renta.Recurso?.Descripcion,
        piso = renta.Recurso?.Zona,
        anfitrionaId = renta.AnfitrionaId,
        anfitriona = renta.Anfitriona?.Nombres,
        nacionalidad = renta.Anfitriona?.Nacionalidad?.Descripcion,
        turno = renta.Turno,
        fechaIngreso = renta.FechaIngreso.ToString(FormatoFechaHora),
        fechaSalida = renta.FechaSalida.HasValue ? renta.FechaSalida.Value.ToString(FormatoFechaHora) : null,
        tarifaCuarto = renta.TarifaCuarto,
        montoTotal = renta.MontoTotal,
        montoCuarto = renta.MontoCuarto,
        montoPendiente = renta.MontoPendiente,
        observaciones = renta.Observaciones,
    };

    public async Task<(ServiceStatus, object?, string)> ListarRentas(string fecha, string turno)
    {
        if (!TryParseFecha(fecha, out var fechaFiltro))
            return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {fecha}");

        try
        {
            var rentas = await QueryRentas()
                .Where(r => r.FechaSalida == null && r.Turno == turno && r.FechaIngreso.Date == fechaFiltro.Date)
                .OrderByDescending(r => r.FechaIngreso)
                .ToListAsync();

            var resultado = rentas.Select(RentaToDto).ToList();

            return (ServiceStatus.Ok, resultado, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar rentas -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ListarRecursosCopados(string turnito)
    {
        try
        {
            var rentas = await QueryRentas()
                .Where(r => r.FechaSalida == null && r.Turno == turnito)
                .ToListAsync();

            var resultado = rentas.Select(r => new
            {
                recursoId = r.RecursoId,
                descripcion = r.Recurso?.Descripcion,
                piso = r.Recurso?.Zona,
                anfitrionaId = r.AnfitrionaId,
                anfitriona = r.Anfitriona?.Nombres,
            }).ToList();

            return (ServiceStatus.Ok, resultado, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar recursos copados -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object)> CrearRenta(CreateRentaPayload payload)
    {
        try
        {
            var recurso = await _context.Recurso.AsNoTracking().FirstOrDefaultAsync(r => r.Id == payload.HabitacionId);

            if (recurso == null)
                return (ServiceStatus.FailedValidation, $"El recurso {payload.HabitacionId} no existe");

            var anfitriona = await _context.Anfitriona.AsNoTracking().FirstOrDefaultAsync(a => a.Id == payload.AnfitrionaId);

            if (anfitriona == null)
                return (ServiceStatus.FailedValidation, $"La anfitriona {payload.AnfitrionaId} no existe");

            var detalles = new List<RentaDetalle>();

            if (payload.DetalleProductos is not null)
            {
                var productoIds = payload.DetalleProductos.Select(d => d.ProductoId).Distinct().ToList();
                var productos = await _context.Producto.AsNoTracking()
                    .Where(p => productoIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var detalle in payload.DetalleProductos)
                {
                    productos.TryGetValue(detalle.ProductoId, out var producto);

                    detalles.Add(new RentaDetalle
                    {
                        ProductoId = detalle.ProductoId,
                        NombreProducto = producto?.Nombre,
                        RutaImagen = producto?.RutaImagen,
                        Precio = detalle.Precio,
                    });
                }
            }

            var renta = new Renta
            {
                RecursoId = payload.HabitacionId,
                AnfitrionaId = payload.AnfitrionaId,
                Turno = payload.Turno,
                FechaIngreso = Ahora(),
                TarifaCuarto = payload.TarifaCuarto,
                MontoTotal = payload.MontoTotal,
                MontoCuarto = payload.MontoCuarto,
                MontoPendiente = payload.MontoPendiente,
                Observaciones = payload.Observaciones,
                Detalles = detalles,
            };

            await _context.Renta.AddAsync(renta);
            await _context.SaveChangesAsync();

            var creada = await QueryRentas().FirstOrDefaultAsync(r => r.Id == renta.Id);

            return (ServiceStatus.Ok, RentaToDto(creada));
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, $"Error crear renta -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ReporteRentas(string fecha, string turno)
    {
        if (!TryParseFecha(fecha, out var fechaFiltro))
            return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {fecha}");

        try
        {
            var rentas = await QueryRentas()
                .Where(r => r.Turno == turno && r.FechaIngreso.Date == fechaFiltro.Date)
                .OrderByDescending(r => r.FechaIngreso)
                .ToListAsync();

            var resultado = rentas.Select(RentaToDto).ToList();

            return (ServiceStatus.Ok, resultado, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar reporte de rentas -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, string)> MarcarSalida(int andfitrionaId, string turno)
    {
        try
        {
            var fechaHoy = Ahora().Date;

            var renta = await _context.Renta
                .Where(r => r.AnfitrionaId == andfitrionaId
                    && r.Turno == turno
                    && r.FechaSalida == null
                    && r.FechaIngreso.Date == fechaHoy)
                .FirstOrDefaultAsync();

            if (renta == null)
                return (ServiceStatus.NotFound, $"No se encontró una renta abierta para la anfitriona {andfitrionaId} en el turno {turno}");

            renta.FechaSalida = Ahora();
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, "Salida marcada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, $"Error al marcar salida -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ListarRecursos()
    {
        try
        {
            await EnsureProductosAsync();

            var recursos = await _context.Recurso.AsNoTracking()
                .Where(r => r.Estado)
                .OrderBy(r => r.Descripcion)
                .Select(r => new
                {
                    id = r.Id,
                    descripcion = r.Descripcion,
                    piso = r.Zona,
                    tipo = r.Tipo,
                })
                .ToListAsync();

            return (ServiceStatus.Ok, recursos, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar recursos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> CompletarDeuda(int idRenta)
    {
        try
        {
            var renta = await _context.Renta.FirstOrDefaultAsync(r => r.Id == idRenta);

            if (renta == null)
                return (ServiceStatus.NotFound, null, $"No se encontró la renta {idRenta}");

            renta.MontoPendiente = 0;

            await _context.SaveChangesAsync();

            var actualizada = await QueryRentas().FirstOrDefaultAsync(r => r.Id == idRenta);

            return (ServiceStatus.Ok, RentaToDto(actualizada), "Deuda completada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al completar deuda -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ListarFichas(string fecha)
    {
        if (!TryParseFecha(fecha, out var fechaFiltro))
            return (ServiceStatus.FailedValidation, null, $"Error en formato de fecha - {fecha}");

        try
        {
            var fichas = await _context.RentaDetalle.AsNoTracking()
                .Include(d => d.Renta)
                .Where(d => d.Renta != null && d.Renta.FechaIngreso.Date == fechaFiltro.Date)
                .Select(d => new
                {
                    id = d.Id,
                    anfitrionaId = d.Renta!.AnfitrionaId,
                    turno = d.Renta!.Turno,
                    comision = d.Precio,
                    fecha = d.Renta.FechaIngreso.ToString(FormatoFechaHora),
                    producto = d.NombreProducto,
                    productoImagen = d.RutaImagen,
                })
                .OrderByDescending(f => f.fecha)
                .ToListAsync();

            return (ServiceStatus.Ok, fichas, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar fichas -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ObtenerConfiguracion()
    {
        try
        {
            var configuracion = await EnsureConfiguracionAsync();
            await EnsureRecursosAsync(configuracion);

            return (ServiceStatus.Ok, ConfiguracionRentaFactory.ConfiguracionToDto(configuracion), "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener configuración -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ActualizarConfiguracion(ConfiguracionRentaPayload payload)
    {
        try
        {
            if (payload.Turnos is null || payload.Tarifas is null || payload.Recursos is null)
                return (ServiceStatus.FailedValidation, null, "La configuración debe incluir turnos, tarifas y recursos");

            var configuracion = await EnsureConfiguracionAsync();

            configuracion.Tipo = payload.Tipo;
            configuracion.TurnosJson = ConfiguracionRentaFactory.SerializarTurnos(payload.Turnos);
            configuracion.TarifasJson = ConfiguracionRentaFactory.SerializarTarifas(payload.Tarifas);
            configuracion.RecursosJson = ConfiguracionRentaFactory.SerializarRecursos(payload.Recursos);

            await _context.SaveChangesAsync();
            await EnsureRecursosAsync(configuracion);

            return (ServiceStatus.Ok, ConfiguracionRentaFactory.ConfiguracionToDto(configuracion), "Configuración actualizada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al actualizar configuración -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}