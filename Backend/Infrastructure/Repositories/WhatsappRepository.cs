using Application.Interfaces.IRepository;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WhatsappRepository : IWhatsappRepository
{
    private readonly SpaContext _context;

    public WhatsappRepository(SpaContext context)
    {
        _context = context;
    }

    public async Task<(ServiceStatus, WhatsappMessage?, string)> RegistrarMensaje(WhatsappMessagePayload payload, string intencion, string estado, string? respuesta)
    {
        try
        {
            var mensaje = new WhatsappMessage
            {
                MessageId = payload.MessageId,
                NumeroOrigen = payload.Numero,
                Texto = payload.Texto,
                Direccion = "IN",
                Intencion = intencion,
                Estado = estado,
                Respuesta = respuesta
            };

            await _context.WhatsappMessage.AddAsync(mensaje);
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, mensaje, "Mensaje registrado");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al registrar mensaje -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<bool> ExisteMensaje(string messageId)
        => await _context.WhatsappMessage.AsNoTracking().AnyAsync(m => m.MessageId == messageId);

    public async Task<WhatsappConversation?> ObtenerConversacion(string numero)
        => await _context.WhatsappConversation.AsNoTracking().FirstOrDefaultAsync(c => c.Numero == numero);

    public async Task<(ServiceStatus, WhatsappConversation?, string)> GuardarConversacion(WhatsappConversation conversacion)
    {
        try
        {
            var existente = await _context.WhatsappConversation.AsTracking().FirstOrDefaultAsync(c => c.Numero == conversacion.Numero);

            if (existente != null)
            {
                existente.Estado = conversacion.Estado;
                existente.ContextoJson = conversacion.ContextoJson;
                existente.UltimoMensaje = conversacion.UltimoMensaje;
            }
            else
            {
                conversacion.UltimoMensaje = DateTime.UtcNow.AddHours(-5);
                await _context.WhatsappConversation.AddAsync(conversacion);
            }

            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, null, "Conversacion guardada");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al guardar conversacion -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}