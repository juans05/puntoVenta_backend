using Application.Interfaces.IRepository;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IComprobanteService
{
    Task<MessageResult<object>> CrearComprobante(ComprobantePayload request);

    Task<MessageResult<object>> ListarComprobantes(ComprobanteQueryParams queryparam);

    Task<MessageResult<object>> VentasRealizadas(string fecha);

    Task<MessageResult<object>> AnularVenta(ComprobanteAnulacionPayload payload);

    Task<MessageResult<string>> GenerarPdf(int idComprobante);

    Task<MessageResult<List<ComprobanteCabecera>>> ListarComprobantesPendientesEnviarSunat(string tenant);

    Task<MessageResult<bool>> ActualizarComprobanteAAnuladoDesdeSunat(int idComprobante, string ticket);

    Task<MessageResult<List<ComprobanteCabecera>>> ListarComprobantesAnulados(string tenant);

    Task<MessageResult<bool>> ActualizarComprobanteAEnviado(int idComprobante, string message);

    Task<MessageResult<bool>> ActualizarComprobanteAError(int idComprobante, string errorMessage);

    Task<MessageResult<CorrelativoAnulacion>> ObtenerCorrelativoAnulacion(string tenant);

    Task<MessageResult<bool>> ActualizarCorrelativoAnulacion(int id);

    Task<MessageResult<List<ConfiguracionFiscal>>> ObtenerConfiguracionesFiscalesActivas();

    Task<MessageResult<ConfiguracionFiscal>> ObtenerConfiguracionFiscalPorTenant(string tenant);

    Task<MessageResult<bool>> ActualizarFechaVenta(int id, DateTime fecha);
}