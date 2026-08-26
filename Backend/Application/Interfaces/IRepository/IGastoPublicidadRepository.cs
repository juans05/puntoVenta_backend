using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IGastoPublicidadRepository
{
    Task<(ServiceStatus, ImportarGastoPublicidadResultDto?, string)> Importar(ImportarGastoPublicidadPayload payload);
    Task<(ServiceStatus, List<RoiPorGrupoDto>?, string)> CalcularRoi(GastoPublicidadRoiQueryParams payload);
    Task<(ServiceStatus, DataCollection<GastoPublicidadDto>?, string)> Listar(GastoPublicidadQueryParams payload);
    Task<(ServiceStatus, List<MapeoAnuncioDto>?, string)> ObtenerMapeosAnuncios(ObtenerMapeosAnunciosPayload payload);
}
