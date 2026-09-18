using Application.Interfaces.IRepository;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IComprobanteService
{
    Task<MessageResult<object>> CrearComprobante(ComprobantePayload request);

    Task<MessageResult<object>> CrearNotaCreditoDebito(NotaPayload request);

    Task<MessageResult<object>> BuscarComprobantePorSerieCorrelativo(string serie, int correlativo);

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

    Task<MessageResult<object>> ObtenerCotizacionParaConvertir(int id);

    Task<MessageResult<string>> GenerarPdfCotizacion(int id);

    Task<MessageResult<object>> ObtenerSeriesDocumento();

    Task<MessageResult<List<LibroVentaDto>>> ObtenerLibroVentas(ContabilidadQueryParams payload);

    Task<MessageResult<List<ReporteDetalladoVentaDto>>> ObtenerReporteDetalladoVentas(ContabilidadQueryParams payload);
}