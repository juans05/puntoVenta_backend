using Application.Interfaces.IRepository;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Jefaturas que aprueban ordenes de compra/servicio sobre el umbral. Un departamento tiene una
// lista de aprobadores; al crear/emitir una orden que necesita aprobacion se elige UNO de ellos
// (ver OrdenCompraRepository) -- este repo solo administra el catalogo de departamentos y quien
// puede aprobar en cada uno.
public class DepartamentoRepository : IDepartamentoRepository
{
    private readonly SpaContext _context;

    public DepartamentoRepository(SpaContext context)
    {
        _context = context;
    }

    private IQueryable<Departamento> Query() => _context.Departamento.AsNoTracking()
        .Include(d => d.Aprobadores).ThenInclude(a => a.User);

    private static DepartamentoDto ToDto(Departamento d) => new()
    {
        Id = d.Id,
        Nombre = d.Nombre,
        Aprobadores = d.Aprobadores.Select(a => new AprobadorDto
        {
            UserId = a.UserId,
            Nombre = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : null
        }).ToList()
    };

    private async Task<string?> ValidarAprobadores(List<string> aprobadorIds)
    {
        if (aprobadorIds.Distinct().Count() != aprobadorIds.Count)
            return "Un usuario no puede repetirse como aprobador";
        if (aprobadorIds.Count == 0) return null;
        var existentes = await _context.Users.AsNoTracking().Where(u => aprobadorIds.Contains(u.Id)).Select(u => u.Id).ToListAsync();
        if (existentes.Count != aprobadorIds.Count)
            return "Algún usuario elegido como aprobador no existe";
        return null;
    }

    public async Task<(ServiceStatus, DepartamentoDto?, string)> Crear(CrearDepartamentoPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Nombre))
            return (ServiceStatus.FailedValidation, null, "El nombre del departamento es obligatorio");
        if (await ValidarAprobadores(payload.AprobadorIds) is { } error)
            return (ServiceStatus.FailedValidation, null, error);

        var departamento = new Departamento
        {
            Nombre = payload.Nombre.Trim(),
            Aprobadores = payload.AprobadorIds.Select(id => new DepartamentoAprobador { UserId = id }).ToList()
        };
        _context.Departamento.Add(departamento);
        await _context.SaveChangesAsync();

        return await Obtener(departamento.Id);
    }

    public async Task<(ServiceStatus, DepartamentoDto?, string)> Actualizar(int id, CrearDepartamentoPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Nombre))
            return (ServiceStatus.FailedValidation, null, "El nombre del departamento es obligatorio");
        if (await ValidarAprobadores(payload.AprobadorIds) is { } error)
            return (ServiceStatus.FailedValidation, null, error);

        var departamento = await _context.Departamento.AsTracking().Include(d => d.Aprobadores).FirstOrDefaultAsync(d => d.Id == id);
        if (departamento == null) return (ServiceStatus.NotFound, null, "Departamento no encontrado");

        departamento.Nombre = payload.Nombre.Trim();
        _context.DepartamentoAprobador.RemoveRange(departamento.Aprobadores);
        departamento.Aprobadores = payload.AprobadorIds.Select(uid => new DepartamentoAprobador { DepartamentoId = id, UserId = uid }).ToList();
        await _context.SaveChangesAsync();

        return await Obtener(id);
    }

    public async Task<(ServiceStatus, DepartamentoDto?, string)> AsignarAprobadores(int id, AsignarAprobadoresPayload payload)
    {
        if (await ValidarAprobadores(payload.AprobadorIds) is { } error)
            return (ServiceStatus.FailedValidation, null, error);

        var departamento = await _context.Departamento.AsTracking().Include(d => d.Aprobadores).FirstOrDefaultAsync(d => d.Id == id);
        if (departamento == null) return (ServiceStatus.NotFound, null, "Departamento no encontrado");

        _context.DepartamentoAprobador.RemoveRange(departamento.Aprobadores);
        departamento.Aprobadores = payload.AprobadorIds.Select(uid => new DepartamentoAprobador { DepartamentoId = id, UserId = uid }).ToList();
        await _context.SaveChangesAsync();

        return await Obtener(id);
    }

    public async Task<(ServiceStatus, List<DepartamentoDto>?, string)> Listar()
    {
        var departamentos = await Query().Where(d => d.Estado).OrderBy(d => d.Nombre).ToListAsync();
        return (ServiceStatus.Ok, departamentos.Select(ToDto).ToList(), "Success");
    }

    public async Task<(ServiceStatus, DepartamentoDto?, string)> Obtener(int id)
    {
        var departamento = await Query().FirstOrDefaultAsync(d => d.Id == id);
        return departamento == null
            ? (ServiceStatus.NotFound, null, "Departamento no encontrado")
            : (ServiceStatus.Ok, ToDto(departamento), "Success");
    }

    public async Task<bool> EsAprobadorDelDepartamento(int departamentoId, string userId)
        => await _context.DepartamentoAprobador.AsNoTracking().AnyAsync(a => a.DepartamentoId == departamentoId && a.UserId == userId);
}
