using Domain.Entities;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IComprobanteRepository
{
    Task<(ServiceStatus, object?, string)> CrearComprobante(ComprobantePayload payload);

    Task<(ServiceStatus, object, string)> ListarComprobantes(ComprobanteQueryParams queryparam);
    Task<(ServiceStatus, object?, string)> VentasRealizadas(string fecha);
    Task<(ServiceStatus, object?, string)> AnularVenta(int IdComprobante, string motivo);

    Task<(ServiceStatus, InvoiceRequest?, string)> GeneratePdfRequest(int idComprobante);

    Task<(ServiceStatus, CorrelativoAnulacion)> ObtenerCorrelativoAnulacion(string tenant);

    Task<(ServiceStatus, bool)> ActualizarCorrelativoAnulacion(int id);

    //JOB
    Task<(ServiceStatus, List<ComprobanteCabecera>?)> ListarComprobantesPendientesEnviarSunat(string tenant);

    Task<(ServiceStatus, bool)> ActualizarComprobanteAAnuladoDesdeSunat(int idComprobante, string ticket);

    Task<(ServiceStatus, List<ComprobanteCabecera>?)> ListarComprobantesAnulados(string tenant);

    //JOB
    Task<(ServiceStatus, bool)> ActualizarComprobanteAEnviado(int idComprobante, string message);

    //JOB
    Task<(ServiceStatus, bool)> ActualizarComprobanteAError(int idComprobante, string errorMessage);

    Task<(ServiceStatus, List<ConfiguracionFiscal>?)> ObtenerConfiguracionesFiscalesActivas();

    Task<(ServiceStatus, ConfiguracionFiscal?)> ObtenerConfiguracionFiscalPorTenant(string tenant);
}