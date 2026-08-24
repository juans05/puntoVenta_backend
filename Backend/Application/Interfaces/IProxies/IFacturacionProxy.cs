using Domain.Models;

namespace Application.Interfaces.IProxies;

public interface IFacturacionProxy
{
    Task<T> EnviarComprobanteSunar<T>(InvoiceRequest cabecera, string? accessToken = null);

    Task<T> GenerarPdf<T>(InvoiceRequest cabecera, string? accessToken = null);

    Task<T> ResumenAnulacion<T>(SummaryRequest cabecera, string? accessToken = null);
}