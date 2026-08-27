using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IGastoRepository
{
    Task<(ServiceStatus, GastoDto?, string)> CrearGasto(CreateGastoPayload payload);

    Task<(ServiceStatus, GastoDto?, string)> AnularGasto(int id);

    Task<(ServiceStatus, string)> ActualizarFechaGasto(int id, DateTime fecha);

    Task<(ServiceStatus, DataCollection<GastoDto>?, string)> ListarGastos(GastoQueryParams payload);

    Task<(ServiceStatus, object?, string)> ListarCategorias();

    Task<(ServiceStatus, object?, string)> CrearCategoria(CreateCategoriaGastoPayload payload);

    Task<(ServiceStatus, string)> CambiarEstadoCategoria(int id, bool estado);
}
