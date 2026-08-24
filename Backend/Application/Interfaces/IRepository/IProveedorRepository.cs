using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
namespace Application.Interfaces.IRepository
{
    public interface IProveedorRepository
    {
        Task<(ServiceStatus, DataCollection<ProveedorDto>?, string)> GetProveedor(ProveedorPayload payload);
        Task<(ServiceStatus, ProveedorDto?, string)> CrearProveedor(CreateProveedorPayload payload);
        Task<(ServiceStatus, ProveedorDto?, string)> UpdateProveedor(UpdateProveedorPayload payload);
        Task<(ServiceStatus, Proveedor?, string)> DeleteProveedor(int ProveedorId);
    }
}
