using System.Text.Json;
using Microsoft.Playwright;
using SimpleApiPlaywright.Core.Types;

namespace SimpleApiPlaywright.Core;

public class ApiClient
{
    private string? _errorMessage;

    private static string? s_appBaseUrl;
    private static int s_initialApiWaitTimeout;
    private static IReadOnlyCollection<int>? s_initialExpectedStatusCodes;
    private static readonly AsyncLocal<ApiContext?> s_currentContext = new();

    private readonly string _apiBaseUrl;
    private readonly string _fullUrl;
    private readonly string _route;
    private readonly ApiHttpMethod _method;
    private readonly IReadOnlyCollection<int> _expectedStatusCodes;
    private readonly int _apiWaitTimeout;
    private readonly object? _body;

    private readonly ApiContext _apiContext;

    public ApiClient(string apiBaseUrl, RequestParameters parameters, ApiContext? apiContext = null)
    {
        _apiContext = apiContext ?? GetContext();

        if (s_appBaseUrl == null)
        {
            throw new Exception(
                $"The {nameof(s_appBaseUrl)} must be set before {nameof(ApiClient)} class initalization"
            );
        }

        if (s_initialExpectedStatusCodes == null && parameters.ExpectedStatusCodes == null)
        {
            throw new Exception(
                $"Expected status codes are not set. Are you sure you used {SetInitialConfig}()?"
            );
        }

        _apiBaseUrl = ConnectUrlParts(s_appBaseUrl, apiBaseUrl);
        _fullUrl = ConnectUrlParts(_apiBaseUrl, parameters.Url ?? string.Empty);
        _route = _fullUrl.Replace(ConnectUrlParts(s_appBaseUrl), string.Empty);
        _method = parameters.Method;
        _expectedStatusCodes = parameters.ExpectedStatusCodes ?? s_initialExpectedStatusCodes!;
        _apiWaitTimeout = parameters.apiWaitTimeout ?? s_initialApiWaitTimeout;
        _body = parameters.Body;
    }

    public static void SetInitialConfig(
        int apiWaitTimeout,
        int[] expectedStatusCodes,
        string baseUrl
    )
    {
        s_initialApiWaitTimeout = apiWaitTimeout;
        s_initialExpectedStatusCodes = expectedStatusCodes;
        s_appBaseUrl = baseUrl;
    }

    public static void SetContext(ApiContext context)
    {
        s_currentContext.Value = context;
    }

    internal static ApiContext GetContext()
    {
        return s_currentContext.Value
            ?? throw new Exception(
                $"ApiContext not set. Are you sure you used {nameof(ApiClient)}.{nameof(SetContext)}()?"
            );
    }

    public async Task<ApiResponse<T>> RequestAsync<T>()
    {
        var response = await RequestBaseAsync();
        return await GetResponseAsync<T>(response);
    }

    public async Task<ApiResponse<dynamic>> RequestAsync()
    {
        var response = await RequestBaseAsync();
        return FormResponse(response);
    }

    private async Task<IAPIResponse> RequestBaseAsync()
    {
        await _apiContext.BrowserContext.Tracing.GroupAsync(
            $"Request {_method} \"{_route}\", expect {string.Join(", ", _expectedStatusCodes)}"
        );

        // Separate bodyJson variable for better debug
        var bodyJson = JsonSerializer.Serialize(_body);
        var response = await _apiContext.Context.FetchAsync(
            _fullUrl,
            new()
            {
                Method = _method.ToString(),
                Data = bodyJson,
                Headers =
                [
                    new("Content-Type", "application/json"),
                    new("Authorization", GetToken() ?? string.Empty),
                ],
                Timeout = _apiWaitTimeout,
            }
        );

        ValidateStatusCode(response.Status);

        await _apiContext.BrowserContext.Tracing.GroupEndAsync();

        return response;
    }

    public async Task<BrowserApiResponse<T>> WaitAsync<T>()
    {
        var response = await WaitBaseAsync<T>();
        return await GetResponseAsync<T>(response);
    }

    public async Task<BrowserApiResponse<dynamic>> WaitAsync()
    {
        var response = await WaitBaseAsync<dynamic>();
        return FormResponse(response);
    }

    public static void SetToken(string token)
    {
        TokenStorage.Set(token);
    }

    private static string? GetToken() => TokenStorage.Get();

    private static string ConnectUrlParts(params string[] parts)
    {
        var connectedParts = string.Join(
            "/",
            parts
                .Where(part => !string.IsNullOrEmpty(part))
                .Select(NormalizeUrl)
                .Where(part => part.Trim().Length > 0)
        );

        return connectedParts;
    }

    private static string NormalizeUrl(string url)
    {
        return RemoveLeadingSlash(RemoveTrailingSlash(url));
    }

    private static string RemoveTrailingSlash(string url)
    {
        return url.EndsWith('/') ? url.Substring(0, url.Length - 1) : url;
    }

    private static string RemoveLeadingSlash(string url)
    {
        return url.StartsWith('/') ? url.Substring(1) : url;
    }

    private async Task<IResponse> WaitBaseAsync<T>()
    {
        if (_apiContext.Page == null)
        {
            throw new PlaywrightException(
                $"You can use {nameof(WaitAsync)}() only in the context of UI Tests (The '{nameof(IPage)}' should be available)."
            );
        }

        await _apiContext.BrowserContext.Tracing.GroupAsync(
            $"Wait for {_method} \"{_route}\" {string.Join(", ", _expectedStatusCodes)}"
        );
        var response = await _apiContext.Page.WaitForResponseAsync(
            (response) =>
            {
                // Ignore trailing slash and casing differences
                var actualUrl = NormalizeUrl(response.Url);
                var expectedUrl = NormalizeUrl(_fullUrl);
                var requestMethod = response.Request.Method;

                if (!actualUrl.Contains(expectedUrl, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }

                if (
                    !requestMethod.Equals(
                        _method.ToString(),
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    return false;
                }

                return true;
            },
            new() { Timeout = _apiWaitTimeout }
        );

        _errorMessage = response.StatusText;

        ValidateStatusCode(response.Status);

        await _apiContext.BrowserContext.Tracing.GroupEndAsync();

        return response;
    }

    private static async Task<ApiResponse<T>> GetResponseAsync<T>(IAPIResponse response)
    {
        // For better debugging convenience created JSON string with TextAsync()
        // and then used JsonSerializer.Deserialize<T>() instead of Playwright's JsonAsync<T>()
        var responseString = await response.TextAsync();
        var responseBody = JsonSerializer.Deserialize<T>(responseString)!;
        return new() { Response = response, ResponseBody = responseBody };
    }

    private static ApiResponse<dynamic> FormResponse(IAPIResponse response)
    {
        return new() { Response = response, ResponseBody = default };
    }

    private static async Task<BrowserApiResponse<T>> GetResponseAsync<T>(IResponse response)
    {
        var responseString = await response.TextAsync();
        var responseBody = JsonSerializer.Deserialize<T>(responseString)!;
        return new() { Response = response, ResponseBody = responseBody };
    }

    private static BrowserApiResponse<dynamic> FormResponse(IResponse response)
    {
        return new() { Response = response, ResponseBody = default };
    }

    private void ValidateStatusCode(int statusCode)
    {
        if (!_expectedStatusCodes.Contains(statusCode))
        {
            throw new Exception(
                $"Expected to return {string.Join(", ", _expectedStatusCodes)}, but got {statusCode}.\nEndpoint: {_method} {_route}\nError Message: {_errorMessage}"
            );
        }
    }
}
