using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository
{
    public interface IRentaRepositorio
    {
        Task<(ServiceStatus, object?, string)> ListarRentas(string fecha, string turno);

        Task<(ServiceStatus, object?, string)> ListarRecursosCopados(string turnito);

        Task<(ServiceStatus, object)> CrearRenta(CreateRentaPayload payload);

        Task<(ServiceStatus, object?, string)> ReporteRentas(string fecha, string turno);

        Task<(ServiceStatus, string)> MarcarSalida(int andfitrionaId, string turno);

        Task<(ServiceStatus, object?, string)> ListarRecursos();

        Task<(ServiceStatus, object?, string)> CompletarDeuda(int idRenta);

        Task<(ServiceStatus, object?, string)> ListarFichas(string fecha);

        Task<(ServiceStatus, object?, string)> ObtenerConfiguracion();

        Task<(ServiceStatus, object?, string)> ActualizarConfiguracion(ConfiguracionRentaPayload payload);
    }
}