using Microsoft.Playwright;
using SimpleApiPlaywright.Types;

namespace SimpleApiPlaywright;

/// <summary>
/// Represents a configured API operation that can be sent directly, observed, or held on a
/// Playwright page.
/// </summary>
/// <typeparam name="T">The expected response body type.</typeparam>
public sealed class ApiAction<T>
{
    private readonly ApiContext _apiContext;
    private readonly ApiClient _apiClient;

    private readonly string _apiBaseUrl;
    private readonly RequestParameters _parameters;

    /// <summary>
    /// Initializes a new API operation.
    /// </summary>
    /// <param name="apiBaseUrl">The API-specific base URL or path.</param>
    /// <param name="parameters">The request and response-matching configuration.</param>
    /// <param name="apiContext">
    /// The Playwright context to use. When omitted, the context configured through
    /// <see cref="ApiClient.SetContext(ApiContext)"/> is used.
    /// </param>
    public ApiAction(string apiBaseUrl, RequestParameters parameters, ApiContext? apiContext = null)
    {
        _apiBaseUrl = apiBaseUrl;
        _parameters = parameters;
        _apiContext = apiContext ?? ApiClient.GetContext();

        _apiClient = new ApiClient(_apiBaseUrl, _parameters, _apiContext);
    }

    /// <summary>
    /// Sends the configured API request and deserializes its response body.
    /// </summary>
    /// <returns>The Playwright API response and its deserialized body.</returns>
    public async Task<ApiResponse<T>> RequestAsync()
    {
        return await _apiClient.RequestAsync<T>();
    }

    /// <summary>
    /// Waits for a matching response on the configured Playwright page and deserializes its body.
    /// Responses with empty or incompatible bodies are ignored.
    /// </summary>
    /// <returns>The matching browser response and its deserialized body.</returns>
    public async Task<BrowserApiResponse<T>> WaitAsync()
    {
        return await _apiClient.WaitAsync<T>();
    }

    /// <summary>
    /// Fetches and holds the next matching browser response until it is released or disposed.
    /// The response body is available on the returned handle without being deserialized.
    /// </summary>
    /// <returns>A held response. Dispose it to abort the request if it is never released.</returns>
    public Task<ApiResponse> HoldAsync() => _apiClient.HoldResponseAsync();
}
