using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IOrdenCompraRepository
{
    Task<(ServiceStatus, ConfiguracionFlujoDto?, string)> ObtenerConfiguracion();
    Task<(ServiceStatus, ConfiguracionFlujoDto?, string)> GuardarConfiguracion(ConfiguracionFlujoPayload payload);

    Task<(ServiceStatus, OrdenCompraDto?, string)> CrearOrden(CreateOrdenCompraPayload payload);
    Task<(ServiceStatus, OrdenCompraDto?, string)> ActualizarOrden(int id, CreateOrdenCompraPayload payload);
    Task<(ServiceStatus, DataCollection<OrdenCompraDto>?, string)> ListarOrdenes(OrdenCompraQueryParams payload);
    Task<(ServiceStatus, OrdenCompraDto?, string)> ObtenerOrden(int id);
    Task<(ServiceStatus, OrdenCompraDto?, string)> EmitirOrden(int id);
    Task<(ServiceStatus, OrdenCompraDto?, string)> AprobarOrden(int id, string? usuario);
    Task<(ServiceStatus, OrdenCompraDto?, string)> AnularOrden(int id);
    Task<(ServiceStatus, OrdenCompraDto?, string)> CerrarOrden(int id, string? motivo);

    Task<(ServiceStatus, OrdenCompraDto?, string)> RegistrarRecepcion(int ordenId, CreateRecepcionPayload payload);
    Task<(ServiceStatus, OrdenCompraDto?, string)> AnularRecepcion(int recepcionId);

    Task<(ServiceStatus, CompraDto?, string)> FacturarOrden(int ordenId, FacturarOrdenCompraPayload payload);
}
