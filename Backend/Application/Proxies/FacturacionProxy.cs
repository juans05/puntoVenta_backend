using Application.Interfaces.IProxies;
using Application.Services.BaseService;
using Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Application.Proxies;

public class FacturacionProxy : BaseService, IFacturacionProxy
{
    private readonly ApiUrl _apiUrl;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;

    public FacturacionProxy(IOptions<ApiUrl> apiurl, IHttpClientFactory httpClientFactory, IConfiguration configuration) : base(httpClientFactory)
    {
        _apiUrl = apiurl.Value;
        _clientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<T> EnviarComprobanteSunar<T>(InvoiceRequest cabecera, string? accessToken = null)

        => await this.SendAsync<T>(new ApiRequest
        {
            apiType = SD.ApiType.POST,
            Url = $"{_apiUrl.BaseUrl}/invoice/send",
            Data = cabecera,
            AccessToken = accessToken ?? _configuration.GetValue<string>("ApiUrl:AccessToken")
        });

    public async Task<T> ResumenAnulacion<T>(SummaryRequest cabecera, string? accessToken = null)

        => await this.SendAsync<T>(new ApiRequest
        {
            apiType = SD.ApiType.POST,
            Url = $"{_apiUrl.BaseUrl}/summary/send",
            Data = cabecera,
            AccessToken = accessToken ?? _configuration.GetValue<string>("ApiUrl:AccessToken")
        });

    public async Task<T> GenerarPdf<T>(InvoiceRequest cabecera, string? accessToken = null)
        
        => await this.SendAsync<T>(new ApiRequest
        {
            apiType = SD.ApiType.POST,
            Url = $"{_apiUrl.BaseUrl}/invoice/pdf",
            Data = cabecera,
            IsDownloadFile = true,
            AccessToken = accessToken ?? _configuration.GetValue<string>("ApiUrl:AccessToken")
        });

}