using Domain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http.Headers;
using System.Text;

namespace Application.Services.BaseService;

public class BaseService : IBaseService
{
    public IHttpClientFactory _httpClientFactory { get; set; }


    public BaseService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    public async Task<T> SendAsync<T>(ApiRequest apiRequest)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Add("Accept", "application/json");

            if (!string.IsNullOrEmpty(apiRequest.ApiKey))
                request.Headers.Add("X-API-Key", apiRequest.ApiKey);

            if (!string.IsNullOrEmpty(apiRequest.Agente))
                request.Headers.Add("Agente", apiRequest.Agente);

            request.RequestUri = new Uri(apiRequest.Url);
            client.DefaultRequestHeaders.Clear();

            if (apiRequest.Data != null)
                request.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data), Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(apiRequest.AccessToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiRequest.AccessToken);

            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Spa", "1.0"));

            client.Timeout = TimeSpan.FromSeconds(20);


            switch (apiRequest.apiType)
            {
                case SD.ApiType.POST:
                    request.Method = HttpMethod.Post;
                    break;
                case SD.ApiType.PUT:
                    request.Method = HttpMethod.Put;
                    break;
                case SD.ApiType.DELETE:
                    request.Method = HttpMethod.Delete;
                    break;
                default:
                    request.Method = HttpMethod.Get;
                    break;
            }

            HttpResponseMessage apiResponse = await client.SendAsync(request);

            if (apiRequest.IsDownloadFile)
            {
                var contentStream = await apiResponse.Content.ReadAsStreamAsync();

                using var memoryStream = new MemoryStream();

                await contentStream.CopyToAsync(memoryStream);

                var arraybase64 = Convert.ToBase64String(memoryStream.ToArray());


                return (T)Convert.ChangeType(arraybase64, typeof(T));
            }
            else
            {
                var apiContent = await apiResponse.Content.ReadAsStringAsync();

                var apiResponseDto = JsonConvert.DeserializeObject<T>(apiContent);

                return apiResponseDto!;
            }

        }
        catch (Exception e)
        {

            var res = JsonConvert.SerializeObject(MessageResult<object>.Of(e.Message, null, code: 3));

            var apiResponseDto = JsonConvert.DeserializeObject<T>(res);

            return apiResponseDto!;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(true);
    }
}
