using Domain.Models;

namespace Application.Services.BaseService;

public interface IBaseService : IDisposable
{
    Task<T> SendAsync<T>(ApiRequest apiRequest);
}
