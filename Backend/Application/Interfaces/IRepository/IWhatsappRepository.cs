using Domain.Entities;
using Domain.Models;
using Domain.Payloads;

namespace Application.Interfaces.IRepository;

public interface IWhatsappRepository
{
    Task<(ServiceStatus, WhatsappMessage?, string)> RegistrarMensaje(WhatsappMessagePayload payload, string intencion, string estado, string? respuesta);

    Task<bool> ExisteMensaje(string messageId);

    Task<WhatsappConversation?> ObtenerConversacion(string numero);

    Task<(ServiceStatus, WhatsappConversation?, string)> GuardarConversacion(WhatsappConversation conversacion);
}