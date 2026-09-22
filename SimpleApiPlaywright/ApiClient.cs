using System.Text.Json;
using Microsoft.Playwright;
using SimpleApiPlaywright.Types;

namespace SimpleApiPlaywright;

/// <summary>
/// Sends API requests and waits for matching responses produced by a Playwright page.
/// </summary>
public sealed class ApiClient
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
    private readonly bool _exactUrlMatch;

    private readonly ApiContext _apiContext;

    /// <summary>
    /// Initializes a client for a single configured endpoint.
    /// </summary>
    /// <param name="apiBaseUrl">The API-specific base URL or path.</param>
    /// <param name="parameters">The request and response-matching configuration.</param>
    /// <param name="apiContext">
    /// The Playwright context to use. When omitted, the context configured through
    /// <see cref="SetContext(ApiContext)"/> is used.
    /// </param>
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
        _exactUrlMatch = parameters.ExactUrlMatch ?? false;
    }

    /// <summary>
    /// Sets the default configuration used by subsequently created clients.
    /// </summary>
    /// <param name="apiWaitTimeout">
    /// The timeout in milliseconds for API requests and response waits. Use <c>0</c> to disable
    /// the timeout.
    /// </param>
    /// <param name="expectedStatusCodes">The response status codes considered successful.</param>
    /// <param name="baseUrl">The application base URL.</param>
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

    /// <summary>
    /// Sets the Playwright context for the current asynchronous execution flow.
    /// </summary>
    /// <param name="context">The context used by clients created without an explicit context.</param>
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

    /// <summary>
    /// Sends the configured API request and deserializes the response body to
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected response body type.</typeparam>
    /// <returns>The Playwright API response and its deserialized body.</returns>
    public async Task<ApiResponse<T>> RequestAsync<T>()
    {
        var response = await RequestBaseAsync();
        return await GetResponseAsync<T>(response);
    }

    /// <summary>
    /// Sends the configured API request without deserializing its response body.
    /// </summary>
    /// <returns>The Playwright API response.</returns>
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

    /// <summary>
    /// Waits for a browser response matching the configured URL and HTTP method whose body can be
    /// deserialized to <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Matching responses with empty, <see langword="null"/>, or incompatible JSON bodies are
    /// ignored. Waiting continues until a compatible response is found or Playwright times out
    /// while waiting for another matching response.
    /// </remarks>
    /// <typeparam name="T">The expected response body type.</typeparam>
    /// <returns>The matching browser response and its deserialized body.</returns>
    public async Task<BrowserApiResponse<T>> WaitAsync<T>()
    {
        while (true)
        {
            var response = await WaitBaseAsync<T>();
            var responseString = await response.TextAsync();

            if (string.IsNullOrWhiteSpace(responseString))
            {
                continue;
            }

            try
            {
                var responseBody = JsonSerializer.Deserialize<T>(responseString);
                if (responseBody is not null)
                {
                    return new() { Response = response, ResponseBody = responseBody };
                }
            }
            catch (JsonException)
            {
                // Continue iteration through APIs until we find one with a correct response body that can be deserialized into passed type T
            }
        }
    }

    /// <summary>
    /// Waits for a browser response matching the configured URL and HTTP method without reading
    /// its response body.
    /// </summary>
    /// <returns>The matching browser response.</returns>
    public async Task<BrowserApiResponse<dynamic>> WaitAsync()
    {
        var response = await WaitBaseAsync<dynamic>();
        return FormResponse(response);
    }

    /// <summary>
    /// Sets the token value used in the <c>Authorization</c> header of direct API requests in the
    /// current asynchronous execution flow.
    /// </summary>
    /// <param name="token">The token value to send.</param>
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

                var isMatch = _exactUrlMatch
                    ? actualUrl.Equals(expectedUrl, StringComparison.Ordinal)
                    : actualUrl.Contains(expectedUrl, StringComparison.InvariantCultureIgnoreCase);

                if (!isMatch)
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
