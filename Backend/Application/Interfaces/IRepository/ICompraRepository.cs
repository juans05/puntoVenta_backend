using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface ICompraRepository
{
    Task<(ServiceStatus, CompraDto?, string)> CrearCompra(CreateCompraPayload payload);

    Task<(ServiceStatus, CompraDto?, string)> AnularCompra(int id);

    Task<(ServiceStatus, DataCollection<CompraDto>?, string)> ListarCompras(CompraQueryParams payload);

    Task<(ServiceStatus, CompraDto?, string)> ObtenerCompra(int id);

    Task<(ServiceStatus, string)> ActualizarFechaCompra(int id, DateTime fecha);
}