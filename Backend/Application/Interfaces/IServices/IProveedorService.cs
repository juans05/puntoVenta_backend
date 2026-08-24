using Domain.Models;
using Domain.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IProveedorService
    {
        Task<MessageResult<object>> CrearProveedor(CreateProveedorPayload payload);
        Task<MessageResult<object>> ModificarProveedor(UpdateProveedorPayload payload);
        Task<MessageResult<object>> EliminarProveedor(int proveedorId);
        Task<MessageResult<object>> ListarProveedor(ProveedorPayload payload);
    }
}
