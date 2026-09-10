using Application.Abstractions;
using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces.IRepository;

public interface IClienteCuentaRepository
{
    Task<(ServiceStatus, ClienteCuenta?, string)> ObtenerPorEmail(string email);
    Task<(ServiceStatus, ClienteCuenta?, string)> ObtenerPorClienteId(int clienteId);
    Task<(ServiceStatus, bool, string)> Crear(ClienteCuenta clienteCuenta);
}