using Microsoft.Playwright;
using SimpleApiPlaywright.Core.Types;

namespace SimpleApiPlaywright.Core;

public class ApiAction<T>
{
    private readonly ApiContext _apiContext;
    private readonly ApiClient _apiClient;

    private readonly string _apiBaseUrl;
    private readonly RequestParameters _parameters;

    public ApiAction(string apiBaseUrl, RequestParameters parameters, ApiContext? apiContext = null)
    {
        _apiBaseUrl = apiBaseUrl;
        _parameters = parameters;
        _apiContext = apiContext ?? ApiClient.GetContext();

        _apiClient = new ApiClient(_apiBaseUrl, _parameters, _apiContext);
    }

    public async Task<ApiResponse<T>> RequestAsync()
    {
        return await _apiClient.RequestAsync<T>();
    }

    public async Task<BrowserApiResponse<T>> WaitAsync()
    {
        return await _apiClient.WaitAsync<T>();
    }
}
