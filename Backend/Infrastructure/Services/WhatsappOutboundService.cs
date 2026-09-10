using Application.Interfaces.IServices;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Services;

public class WhatsappOutboundService : IWhatsappOutboundService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public WhatsappOutboundService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<bool> EnviarTexto(string numero, string mensaje)
    {
        try
        {
            var accessToken = _configuration["WhatsApp:AccessToken"];
            var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            var version = _configuration["WhatsApp:ApiVersion"] ?? "v18.0";

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(phoneNumberId))
            {
                // Credenciales no configuradas - log y retorno false
                return false;
            }

            var url = $"https://graph.facebook.com/{version}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = numero,
                type = "text",
                text = new { body = mensaje }
            };

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            // Log error
            return false;
        }
        catch
        {
            return false;
        }
    }
}