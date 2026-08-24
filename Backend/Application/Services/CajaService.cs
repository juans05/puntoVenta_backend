using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;


public class CajaService : ICajaService
{
    private readonly ICajaRepository _cajaRepository;

    public CajaService(ICajaRepository cajaRepository)
    {
        this._cajaRepository = cajaRepository;
    }

    public async Task<MessageResult<object>> MontoActual(string usuario, int? sucursalId = null)
    {

        var (estado, resp, message) = await _cajaRepository.MontoActual(usuario, sucursalId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, null, internalResponse: 105);

        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<object>> AbrirCaja(string monto, int? sucursalId = null)
    {

        var (estado, obj, message) = await _cajaRepository.AbrirCaja(monto, sucursalId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, obj);
    }
    public async Task<MessageResult<object>> Retiro(CreateRetiroPayload payload)
    {

        var (estado, obj, message) = await _cajaRepository.Retiro(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, obj);
    }
    public async Task<MessageResult<object>> CerrarCaja(int? sucursalId = null)
    {

        var (estado, RESPO, message) = await _cajaRepository.CerrarCaja(sucursalId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, RESPO);
    }

    public async Task<MessageResult<object>> ReporteCaja(string usuario, string fecha, int? sucursalId = null)
    {

        var (estado, resp, message) = await _cajaRepository.ReporteCaja(usuario, fecha, sucursalId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, resp);
    }
    public async Task<MessageResult<object>> ReporteCajaResumido(string usuario, string fecha, int? sucursalId = null)
    {

        var (estado, resp, message) = await _cajaRepository.ReporteCajaResumido(usuario, fecha, sucursalId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, resp);
    }
    public async Task<MessageResult<object>> HistoricoCierreCajaUsuario(string fecha, int? sucursalId = null)
    {

        var (estado, resp, message) = await _cajaRepository.HistoricoCierreCajaUsuario(fecha, sucursalId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<object>> ListarCajas()
    {

        var (estado, resp, message) = await _cajaRepository.ListarCajas();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, resp);
    }

    public async Task<MessageResult<object>> CrearCaja(CreateCajaPayload payload)
    {

        var (estado, resp, message) = await _cajaRepository.CrearCaja(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                    estado == ServiceStatus.FailedValidation
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.InternalServerError
                , message, null);

        return MessageResult<object>.Of(message, resp);
    }


}
