using Domain.DTO;
using Domain.Payloads;

namespace Application.Interfaces.IServices;

public interface IWhatsappService
{
    Task<IntentResult> ProcesarMensaje(WhatsappMessagePayload payload);
}