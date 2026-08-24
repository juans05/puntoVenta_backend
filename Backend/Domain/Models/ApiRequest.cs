using static Domain.Models.SD;

namespace Domain.Models;

public class ApiRequest
{
    public ApiType apiType { get; set; } = ApiType.GET;
    public string Url { get; set; }
    public object Data { get; set; }
    public string AccessToken { get; set; }
    public string ApiKey { get; set; }
    public string Agente { get; set; }
    public bool IsDownloadFile { get; set; } = false;
}
