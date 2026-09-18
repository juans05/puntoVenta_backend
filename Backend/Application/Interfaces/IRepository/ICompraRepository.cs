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

    Task<(ServiceStatus, CompraDto?, string)> ActualizarCompra(int id, CreateCompraPayload payload);

    Task<(ServiceStatus, CompraXmlPreviewDto?, string)> ImportarXmlCompra(Stream xmlStream);

    Task<(ServiceStatus, List<LibroCompraDto>?, string)> ObtenerLibroCompras(ContabilidadQueryParams payload);

    Task<(ServiceStatus, List<ReporteDetalladoCompraDto>?, string)> ObtenerReporteDetalladoCompras(ContabilidadQueryParams payload);
}