using Application.Interfaces.IProxies;
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;


public class ComprobanteService : IComprobanteService
{
    private readonly IComprobanteRepository _comprobanteRepository;

    private readonly IFacturacionProxy _facturacionProxy;

    public ComprobanteService(IComprobanteRepository comprobanteRepository, IFacturacionProxy facturacionProxy)
    {
        this._comprobanteRepository = comprobanteRepository;
        _facturacionProxy = facturacionProxy;
    }

    public async Task<MessageResult<object>> CrearComprobante(ComprobantePayload request)
    {

        var (estado, resp, message) = await _comprobanteRepository.CrearComprobante(request);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, resp);

    }
    public async Task<MessageResult<object>> ListarComprobantes(ComprobanteQueryParams queryparam)
    {

        var (estado, entity, message) = await _comprobanteRepository.ListarComprobantes(queryparam);

        if (estado == ServiceStatus.FailedValidation)
            throw new ErrorHandler(HttpStatusCode.BadRequest, message, entity, status: 400);

        if (estado == ServiceStatus.NotFound)
            throw new ErrorHandler(HttpStatusCode.NotFound, message, entity, status: 404);

        return MessageResult<object>.Of(message, entity);

    }
    public async Task<MessageResult<object>> VentasRealizadas(string fecha)
    {

        var (estado, entity, message) = await _comprobanteRepository.VentasRealizadas(fecha);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, entity);

        return MessageResult<object>.Of(message, entity);

    }

    public async Task<MessageResult<string>> GenerarPdf(int idComprobante)
    {

        var (estado, entity, message) = await _comprobanteRepository.GeneratePdfRequest(idComprobante);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, entity);


        var result = await _facturacionProxy.GenerarPdf<string>(entity);

        return MessageResult<string>.Of(message, result);

    }

    public async Task<MessageResult<object>> AnularVenta(ComprobanteAnulacionPayload payload)
    {

        var (estado, result, message) = await _comprobanteRepository.AnularVenta(payload.idComprobante, payload.MotivoAnulacion);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , "Error al Anular", message);

        return MessageResult<object>.Of(message, result);

    }

    //JOB
    public async Task<MessageResult<List<ComprobanteCabecera>>> ListarComprobantesPendientesEnviarSunat(string tenant)
    {

        var (estado, resp) = await _comprobanteRepository.ListarComprobantesPendientesEnviarSunat(tenant);


        return MessageResult<List<ComprobanteCabecera>>.Of("SUCCESS", resp);

    }

    public async Task<MessageResult<List<ComprobanteCabecera>>> ListarComprobantesAnulados(string tenant)
    {

        var (estado, resp) = await _comprobanteRepository.ListarComprobantesAnulados(tenant);


        return MessageResult<List<ComprobanteCabecera>>.Of("SUCCESS", resp);

    }
    public async Task<MessageResult<bool>> ActualizarComprobanteAEnviado(int idComprobante, string message)
    {

        var (estado, resp) = await _comprobanteRepository.ActualizarComprobanteAEnviado(idComprobante, message);


        return MessageResult<bool>.Of("SUCCESS", resp);
    }

    public async Task<MessageResult<bool>> ActualizarComprobanteAAnuladoDesdeSunat(int idComprobante, string ticket)
    {

        var (estado, resp) = await _comprobanteRepository.ActualizarComprobanteAAnuladoDesdeSunat(idComprobante, ticket);


        return MessageResult<bool>.Of("SUCCESS", resp);
    }


    public async Task<MessageResult<CorrelativoAnulacion>> ObtenerCorrelativoAnulacion(string tenant)
    {

        var (estado, resp) = await _comprobanteRepository.ObtenerCorrelativoAnulacion(tenant);


        return MessageResult<CorrelativoAnulacion>.Of("SUCCESS", resp);
    }

    public async Task<MessageResult<bool>> ActualizarCorrelativoAnulacion(int id)
    {

        var (estado, resp) = await _comprobanteRepository.ActualizarCorrelativoAnulacion(id);


        return MessageResult<bool>.Of("SUCCESS", resp);
    }


    public async Task<MessageResult<bool>> ActualizarComprobanteAError(int idComprobante, string errorMessage)
    {

        var (estado, resp) = await _comprobanteRepository.ActualizarComprobanteAError(idComprobante, errorMessage);


        return MessageResult<bool>.Of("SUCCESS", resp);
    }

    public async Task<MessageResult<List<ConfiguracionFiscal>>> ObtenerConfiguracionesFiscalesActivas()
    {

        var (estado, resp) = await _comprobanteRepository.ObtenerConfiguracionesFiscalesActivas();


        return MessageResult<List<ConfiguracionFiscal>>.Of("SUCCESS", resp);
    }

    public async Task<MessageResult<ConfiguracionFiscal>> ObtenerConfiguracionFiscalPorTenant(string tenant)
    {

        var (estado, resp) = await _comprobanteRepository.ObtenerConfiguracionFiscalPorTenant(tenant);


        return MessageResult<ConfiguracionFiscal>.Of("SUCCESS", resp);
    }

    public async Task<MessageResult<bool>> ActualizarFechaVenta(int id, DateTime fecha)
    {
        var (estado, message) = await _comprobanteRepository.ActualizarFechaVenta(id, fecha);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                        ? HttpStatusCode.NotFound
                        : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<bool>.Of(message, true);
    }
}
