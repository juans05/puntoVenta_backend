namespace Application.Interfaces.IServices;

public interface IWhatsappOutboundService
{
    Task<bool> EnviarTexto(string numero, string mensaje);
}