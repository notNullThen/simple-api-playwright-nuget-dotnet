namespace SimpleApiPlaywright.Types;

/// <summary>
/// Configures an API request and the criteria used to match a browser response.
/// </summary>
public sealed class RequestParameters
{
    /// <summary>
    /// The relative URL path or absolute URL of the API endpoint.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The HTTP method used for the request or response match.
    /// </summary>
    public ApiHttpMethod Method;

    /// <summary>
    /// The accepted response status codes. An unexpected status code causes an exception.
    /// </summary>
    public int[]? ExpectedStatusCodes { get; set; }

    /// <summary>
    /// The request payload/body to send.
    /// </summary>
    public object? Body { get; set; }

    /// <summary>
    /// The timeout in milliseconds for sending the request or waiting for a response. When omitted,
    /// the value configured through <see cref="ApiClient.SetInitialConfig(int, int[], string)"/> is
    /// used. Use <c>0</c> to disable the timeout.
    /// </summary>
    public int? apiWaitTimeout;

    /// <summary>
    /// Whether browser responses must match the complete URL instead of containing the configured
    /// URL. Leading and trailing slashes are ignored. The default is <see langword="false"/>.
    /// </summary>
    public bool? ExactUrlMatch { get; set; }
}
