using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository
{
    public interface ITenantRepository
    {
        Task<(ServiceStatus, object?, string)> GetTenantsResumen();

        Task<(ServiceStatus, string)> SetTenantActivo(int identificador, bool activo);

        Task<(ServiceStatus, string)> ReasignarModulos(int identificador);

        Task<(ServiceStatus, int, string)> CreateTenant(string nombre, int Rubro, ConfiguracionRentaPayload? configuracion);

        Task<(ServiceStatus, object?, string)> GetConfiguracionRubro(int rubroId);

        Task<(ServiceStatus, object?, string)> SaveConfiguracionRubro(int rubroId, ConfiguracionRentaPayload payload);


        Task<(ServiceStatus, object?, string)> CreateEmpresa(CreateEmpresaPayload payload);
        Task<(ServiceStatus, Empresa?, string)> UpdateTenant(UpdateTenantPayload payload);

        Task<(ServiceStatus, string)> AddEmpresaTenant(AddEmpresaPayload payload);


        Task<(ServiceStatus, List<Empresa>?, string)> GetTenants();

        Task<(ServiceStatus, object?, string)> GetEmpresa(string tenantNombre);

        Task<(ServiceStatus, object?, string)> GetRecursos(string tenant);

        Task<(ServiceStatus, object?, string)> GetAllTenants();
    }
}
