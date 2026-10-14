using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Playwright;
using SimpleApiPlaywright.Types;

namespace SimpleApiPlaywright;

/// <summary>
/// Sends API requests, waits for browser responses, and holds responses for later release.
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
    /// Holds the next matching browser response until it is released or disposed.
    /// </summary>
    /// <returns>The held response.</returns>
    public async Task<ApiResponse> HoldResponseAsync()
    {
        var page =
            _apiContext.Page
            ?? throw new PlaywrightException(
                $"You can use {nameof(HoldResponseAsync)}() only in UI tests with an {nameof(IPage)}."
            );
        var timeout =
            _apiWaitTimeout == 0
                ? Timeout.InfiniteTimeSpan
                : TimeSpan.FromMilliseconds(_apiWaitTimeout);
        var captured = new TaskCompletionSource<ApiResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var pendingRoutes = new ConcurrentBag<Task>();
        Func<string, bool> matchesUrl = url => MatchesUrl(url, _fullUrl);
        Func<IRoute, Task> handler = null!;

        async Task RemoveRouteAsync()
        {
            // Finish other intercepted requests before removing the handler to avoid replaying them.
            await Task.WhenAll(pendingRoutes);
            if (!page.IsClosed)
            {
                await page.UnrouteAsync(matchesUrl, handler);
            }
        }

        handler = async route =>
        {
            var finished = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            pendingRoutes.Add(finished.Task);
            IAPIResponse? response = null;
            try
            {
                if (captured.Task.IsCompleted || !MatchesMethod(route.Request))
                {
                    await route.FallbackAsync();
                    return;
                }

                response = await route.FetchAsync(
                    new() { Timeout = _apiWaitTimeout, MaxRedirects = 0 }
                );
                var handle = new ApiResponse(route, response, timeout, RemoveRouteAsync);
                if (_expectedStatusCodes.Contains(response.Status) && captured.TrySetResult(handle))
                {
                    response = null;
                    finished.TrySetResult();
                    await handle.Completion;
                    return;
                }

                await route.FulfillAsync(new() { Response = response });
            }
            catch (Exception exception)
            {
                captured.TrySetException(exception);
                if (!page.IsClosed && route.Request.Failure == null)
                {
                    await route.AbortAsync();
                }
            }
            finally
            {
                try
                {
                    if (response != null)
                    {
                        await response.DisposeAsync();
                    }
                }
                finally
                {
                    finished.TrySetResult();
                }
            }
        };

        await page.RouteAsync(matchesUrl, handler);
        try
        {
            return await captured.Task.WaitAsync(timeout);
        }
        catch
        {
            captured.TrySetCanceled();
            if (captured.Task.IsCompletedSuccessfully)
            {
                await captured.Task.Result.DisposeAsync();
            }
            else
            {
                await RemoveRouteAsync();
            }
            throw;
        }
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
                return MatchesUrl(response.Url, _fullUrl) && MatchesMethod(response.Request);
            },
            new() { Timeout = _apiWaitTimeout }
        );

        _errorMessage = response.StatusText;

        ValidateStatusCode(response.Status);

        await _apiContext.BrowserContext.Tracing.GroupEndAsync();

        return response;
    }

    private bool MatchesUrl(string url, string expectedUrl)
    {
        var actual = NormalizeUrl(url);
        var expected = NormalizeUrl(expectedUrl);
        return _exactUrlMatch
            ? actual.Equals(expected, StringComparison.Ordinal)
            : actual.Contains(expected, StringComparison.InvariantCultureIgnoreCase);
    }

    private bool MatchesMethod(IRequest request) =>
        request.Method.Equals(_method.ToString(), StringComparison.InvariantCultureIgnoreCase);

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
