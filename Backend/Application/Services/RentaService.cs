using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services;

public class RentaService : IRentaService
{
    private readonly IRentaRepositorio _rentaRepositorio;

    public RentaService(IRentaRepositorio rentaRepositorio)
    {
        _rentaRepositorio = rentaRepositorio;
    }

    public async Task<MessageResult<object>> ListarRentas(string fecha, string turno)
    {
        var (estado, result, message) = await _rentaRepositorio.ListarRentas(fecha, turno);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al listar rentas", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ListarRecursosCopados(string turno)
    {
        var (estado, result, message) = await _rentaRepositorio.ListarRecursosCopados(turno);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al listar recursos copados", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> CrearRenta(CreateRentaPayload payload)
    {
        var (estado, result) = await _rentaRepositorio.CrearRenta(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(HttpStatusCode.BadRequest, "Error al crear renta", result);

        return MessageResult<object>.Of("Renta creada correctamente", result);
    }

    public async Task<MessageResult<object>> ReporteRentas(string fecha, string turno)
    {
        var (estado, result, message) = await _rentaRepositorio.ReporteRentas(fecha, turno);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al consultar reporte de rentas", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> MarcarSalida(int anfitrionaId, string turno)
    {
        var (estado, message) = await _rentaRepositorio.MarcarSalida(anfitrionaId, turno);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al marcar salida", message);

        return MessageResult<object>.Of(message, null);
    }

    public async Task<MessageResult<object>> ListarRecursos()
    {
        var (estado, result, message) = await _rentaRepositorio.ListarRecursos();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al listar recursos", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ObtenerConfiguracion()
    {
        var (estado, result, message) = await _rentaRepositorio.ObtenerConfiguracion();

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al obtener configuración", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ActualizarConfiguracion(ConfiguracionRentaPayload payload)
    {
        var (estado, result, message) = await _rentaRepositorio.ActualizarConfiguracion(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al actualizar configuración", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> CompletarDeuda(int idRenta)
    {
        var (estado, result, message) = await _rentaRepositorio.CompletarDeuda(idRenta);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al completar deuda", message);

        return MessageResult<object>.Of(message, result);
    }

    public async Task<MessageResult<object>> ListarFichas(string fecha)
    {
        var (estado, result, message) = await _rentaRepositorio.ListarFichas(fecha);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(ErrorStatus(estado), "Error al listar fichas", message);

        return MessageResult<object>.Of(message, result);
    }

    private static HttpStatusCode ErrorStatus(ServiceStatus estado) => estado switch
    {
        ServiceStatus.NotFound => HttpStatusCode.NotFound,
        ServiceStatus.FailedValidation => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError,
    };
}