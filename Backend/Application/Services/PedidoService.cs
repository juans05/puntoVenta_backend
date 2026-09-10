using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Common;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;
using System.Net;
using Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IWhatsappOutboundService _whatsappOutboundService;

    public PedidoService(IPedidoRepository pedidoRepository, IWhatsappOutboundService whatsappOutboundService)
    {
        _pedidoRepository = pedidoRepository;
        _whatsappOutboundService = whatsappOutboundService;
    }

    public async Task<MessageResult<PedidoDTO>> CrearPedido(CreatePedidoPayload payload)
    {
        var (estado, result, message) = await _pedidoRepository.CrearPedido(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.FailedValidation
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError
            , "Error al crear pedido", message);

        return MessageResult<PedidoDTO>.Of(message, result);
    }

    public async Task<MessageResult<PedidoDTO>> ObtenerPedidoPorId(int id)
    {
        var (estado, result, message) = await _pedidoRepository.ObtenerPedidoPorId(id);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError
            , message, result);

        return MessageResult<PedidoDTO>.Of(message, result);
    }

    public async Task<MessageResult<PedidoPublicoDTO>> ObtenerPedidoPublico(string token)
    {
        var (estado, result, message) = await _pedidoRepository.ObtenerPedidoPublico(token);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError
            , message, result);

        return MessageResult<PedidoPublicoDTO>.Of(message, result);
    }

    public async Task<MessageResult<DataCollection<PedidoDTO>>> ListarPedidos(PedidoQueryParams payload)
    {
        var (estado, result, message) = await _pedidoRepository.ListarPedidos(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError
            , message, result);

        return MessageResult<DataCollection<PedidoDTO>>.Of(message, result);
    }

    public async Task<MessageResult<PedidoDTO>> ActualizarEstadoPedido(UpdatePedidoEstadoPayload payload)
    {
        var (estado, result, message) = await _pedidoRepository.ActualizarEstadoPedido(payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.FailedValidation
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError
            , "Error al actualizar estado", message);

        return MessageResult<PedidoDTO>.Of(message, result);
    }

    public async Task<MessageResult<bool>> ReenviarPassword(int pedidoId)
    {
        var (estado, result, message) = await _pedidoRepository.ReenviarPassword(pedidoId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.FailedValidation
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError
            , message, result);

        // Obtener el pedido para enviar el WhatsApp
        var (estadoPedido, pedido, _) = await _pedidoRepository.ObtenerPedidoPorId(pedidoId);
        
        if (estadoPedido == ServiceStatus.Ok && pedido != null && !string.IsNullOrEmpty(pedido.Celular))
        {
            // Necesitaríamos obtener el password real para reenviar
            // Por ahora solo marcamos que se puede reenviar
            await _whatsappOutboundService.EnviarTexto(pedido.Celular!, 
                $"Tu contraseña para acceder a tu pedido #{pedido.Id} es: [PASSWORD_NO_DISPONIBLE]. Por favor contacta al negocio.");
        }

        return MessageResult<bool>.Of(message, result);
    }

    public async Task<MessageResult<EtiquetaEnvioDTO>> ObtenerEtiquetaEnvio(int pedidoId)
    {
        var (estado, result, message) = await _pedidoRepository.ObtenerEtiquetaEnvio(pedidoId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError
            , message, result);

        return MessageResult<EtiquetaEnvioDTO>.Of(message, result);
    }

    public async Task<MessageResult<bool>> SubmitPedidoPublico(string token, PedidoPublicoSubmitPayload payload)
    {
        var (estado, result, message) = await _pedidoRepository.SubmitPedidoPublico(token, payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.FailedValidation
                ? HttpStatusCode.BadRequest
                : estado == ServiceStatus.NotFound
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError
            , message, result.Exito);

        // Si se generó un password nuevo, enviarlo por WhatsApp
        if (result.Exito && !string.IsNullOrEmpty(result.PasswordGenerado) && !string.IsNullOrEmpty(result.Celular))
        {
            var enviado = await _whatsappOutboundService.EnviarTexto(result.Celular!, 
                $"Gracias por completar tus datos. Tu contraseña para acceder a tu pedido es: {result.PasswordGenerado}. Guárdala para futuros accesos.");
            
            // Actualizar PasswordEnviado en el pedido
            await _pedidoRepository.ActualizarPasswordEnviado(result.PedidoId, enviado);
        }
        else if (result.Exito && result.CuentaExistia)
        {
            // Si la cuenta ya existía, marcar como enviado (no se reenvía password)
            await _pedidoRepository.ActualizarPasswordEnviado(result.PedidoId, true);
        }

        return MessageResult<bool>.Of(message, result.Exito);
    }

    public async Task<MessageResult<DataCollection<PedidoDTO>>> ListarPedidosCliente(int clienteId, PaginationPayload payload)
    {
        var (estado, result, message) = await _pedidoRepository.ListarPedidosCliente(clienteId, payload);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError
            , message, result);

        return MessageResult<DataCollection<PedidoDTO>>.Of(message, result);
    }

    public async Task<MessageResult<PedidoDTO>> ObtenerPedidoCliente(int clienteId, int pedidoId)
    {
        var (estado, result, message) = await _pedidoRepository.ObtenerPedidoCliente(clienteId, pedidoId);

        if (estado != ServiceStatus.Ok)
            throw new ErrorHandler(
                estado == ServiceStatus.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError
            , message, result);

        return MessageResult<PedidoDTO>.Of(message, result);
    }
}