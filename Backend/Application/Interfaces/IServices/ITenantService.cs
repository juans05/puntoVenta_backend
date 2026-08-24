using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface ITenantService
{

    Task<MessageResult<int>> CreateTenant(string nombre, int Rubro, ConfiguracionRentaPayload? configuracion);

    Task<MessageResult<object>> GetConfiguracionRubro(int rubroId);

    Task<MessageResult<object>> SaveConfiguracionRubro(int rubroId, ConfiguracionRentaPayload payload);

    Task<MessageResult<bool>> AddEmpresaTenant(AddEmpresaPayload payload);
    Task<MessageResult<object>> CreateEmpresa(CreateEmpresaPayload payload);
    Task<MessageResult<object>> ModificarTenant(UpdateTenantPayload payload);
    Task<MessageResult<object>> ListarTenants();

    Task<MessageResult<object>> GetEmpresa(string tenantNombre);

    Task<MessageResult<object?>> GetRecursos(string tenant);

    Task<MessageResult<object>> GetAllTenants();

    Task<MessageResult<object>> GetTenantsResumen();

    Task<MessageResult<bool>> SetTenantActivo(int identificador, bool activo);

    Task<MessageResult<bool>> ReasignarModulos(int identificador);
}
