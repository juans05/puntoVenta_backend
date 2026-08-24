using Application.Interfaces.IRepository;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AnfitrionaRepository : IAnfitrionaRepository
{
    private readonly SpaContext _context;

    public AnfitrionaRepository(SpaContext context)
    {
        _context = context;
    }

    private static object AnfitrionaToDto(Anfitriona anfitriona) => new
    {
        anfitrionaId = anfitriona.Id,
        nombres = anfitriona.Nombres,
        apellidos = anfitriona.Apellidos,
        nacionalidadId = anfitriona.NacionalidadId,
        nacionalidadDescripcion = anfitriona.Nacionalidad?.Descripcion,
        direccion = anfitriona.Direccion,
        celular = anfitriona.Celular,
        foto = anfitriona.Foto,
        estado = anfitriona.Estado,
    };

    public async Task<(ServiceStatus, object?, string)> ListarAnfitrionas(int page, int amount)
    {
        if (page <= 0) page = 1;
        if (amount <= 0) amount = 100;

        try
        {
            var query = _context.Anfitriona
                .Include(a => a.Nacionalidad)
                .AsNoTracking()
                .Where(a => a.Estado);

            var total = await query.CountAsync();

            var anfitrionas = await query
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * amount)
                .Take(amount)
                .ToListAsync();

            var items = anfitrionas.Select(AnfitrionaToDto).ToList();

            return (ServiceStatus.Ok, new { items, total }, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar anfitrionas -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> CrearAnfitriona(CreateAnfitrionaPayload payload)
    {
        try
        {
            var anfitriona = new Anfitriona
            {
                Nombres = payload.Nombres,
                Apellidos = payload.Apellidos,
                NacionalidadId = payload.NacionalidadId > 0 ? payload.NacionalidadId : null,
                Direccion = payload.Direccion,
                Celular = payload.Celular,
                Foto = payload.Foto,
            };

            await _context.Anfitriona.AddAsync(anfitriona);
            await _context.SaveChangesAsync();

            var creada = await _context.Anfitriona
                .Include(a => a.Nacionalidad)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == anfitriona.Id);

            return (ServiceStatus.Ok, AnfitrionaToDto(creada), "Anfitriona creada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error crear anfitriona -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> ActualizarAnfitriona(UpdateAnfitrionaPayload payload)
    {
        try
        {
            var anfitriona = await _context.Anfitriona.FirstOrDefaultAsync(a => a.Id == payload.AnfitrionaId);

            if (anfitriona == null)
                return (ServiceStatus.NotFound, null, $"No se encontró la anfitriona {payload.AnfitrionaId}");

            anfitriona.Nombres = payload.Nombres;
            anfitriona.Apellidos = payload.Apellidos;
            anfitriona.NacionalidadId = payload.NacionalidadId > 0 ? payload.NacionalidadId : null;
            anfitriona.Direccion = payload.Direccion;
            anfitriona.Celular = payload.Celular;
            anfitriona.Foto = payload.Foto;
            anfitriona.UsuarioCreacion = payload.UsuarioModificacion ?? anfitriona.UsuarioCreacion;

            await _context.SaveChangesAsync();

            var actualizada = await _context.Anfitriona
                .Include(a => a.Nacionalidad)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == anfitriona.Id);

            return (ServiceStatus.Ok, AnfitrionaToDto(actualizada), "Anfitriona actualizada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error actualizar anfitriona -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, object?, string)> EliminarAnfitriona(int idAnfitriona)
    {
        try
        {
            var anfitriona = await _context.Anfitriona.FirstOrDefaultAsync(a => a.Id == idAnfitriona);

            if (anfitriona == null)
                return (ServiceStatus.NotFound, null, $"No se encontró la anfitriona {idAnfitriona}");

            anfitriona.Estado = false;

            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, idAnfitriona, "Anfitriona eliminada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error eliminar anfitriona -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}