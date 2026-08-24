using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices
{
    public interface IRentaService
    {
        Task<MessageResult<object>> ListarRentas(string fecha, string turno);

        Task<MessageResult<object>> ListarRecursosCopados(string turno);

        Task<MessageResult<object>> CrearRenta(CreateRentaPayload payload);

        Task<MessageResult<object>> ReporteRentas(string fecha, string turno);

        Task<MessageResult<object>> MarcarSalida(int anfitrionaId, string turno);

        Task<MessageResult<object>> ListarRecursos();

        Task<MessageResult<object>> CompletarDeuda(int idRenta);

        Task<MessageResult<object>> ListarFichas(string fecha);

        Task<MessageResult<object>> ObtenerConfiguracion();

        Task<MessageResult<object>> ActualizarConfiguracion(ConfiguracionRentaPayload payload);
    }
}