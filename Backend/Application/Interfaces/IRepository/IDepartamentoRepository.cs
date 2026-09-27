using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IDepartamentoRepository
{
    Task<(ServiceStatus, DepartamentoDto?, string)> Crear(CrearDepartamentoPayload payload);
    Task<(ServiceStatus, DepartamentoDto?, string)> Actualizar(int id, CrearDepartamentoPayload payload);
    Task<(ServiceStatus, List<DepartamentoDto>?, string)> Listar();
    Task<(ServiceStatus, DepartamentoDto?, string)> Obtener(int id);
    Task<(ServiceStatus, DepartamentoDto?, string)> AsignarAprobadores(int id, AsignarAprobadoresPayload payload);
    // Usado por OrdenCompraRepository para validar que el aprobador elegido pertenezca al departamento.
    Task<bool> EsAprobadorDelDepartamento(int departamentoId, string userId);
}
