using Application.Interfaces.IRepository;
using Domain.Entities;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ClienteCuentaRepository : IClienteCuentaRepository
{
    private readonly SpaContext _context;

    public ClienteCuentaRepository(SpaContext context)
    {
        _context = context;
    }

    public async Task<(ServiceStatus, ClienteCuenta?, string)> ObtenerPorEmail(string email)
    {
        try
        {
            var cuenta = await _context.Set<ClienteCuenta>()
                .Include(cc => cc.Cliente)
                .FirstOrDefaultAsync(cc => cc.Email == email);

            if (cuenta == null)
                return (ServiceStatus.NotFound, null, "Cuenta no encontrada");

            return (ServiceStatus.Ok, cuenta, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, ClienteCuenta?, string)> ObtenerPorClienteId(int clienteId)
    {
        try
        {
            var cuenta = await _context.Set<ClienteCuenta>()
                .Include(cc => cc.Cliente)
                .FirstOrDefaultAsync(cc => cc.ClienteId == clienteId);

            if (cuenta == null)
                return (ServiceStatus.NotFound, null, "Cuenta no encontrada");

            return (ServiceStatus.Ok, cuenta, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, bool, string)> Crear(ClienteCuenta clienteCuenta)
    {
        try
        {
            await _context.Set<ClienteCuenta>().AddAsync(clienteCuenta);
            await _context.SaveChangesAsync();
            return (ServiceStatus.Ok, true, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, false, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}