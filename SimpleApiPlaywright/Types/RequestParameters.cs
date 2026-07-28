namespace SimpleApiPlaywright.Types;

public sealed class RequestParameters
{
    /// <summary>
    /// The relative URL path or absolute URL of the API endpoint.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The HTTP method to be used for the request.
    /// </summary>
    public ApiHttpMethod Method;

    /// <summary>
    /// List of expected HTTP status codes. If the response status is not in this list, an exception is thrown.
    /// </summary>
    public int[]? ExpectedStatusCodes { get; set; }

    /// <summary>
    /// The request payload/body to send.
    /// </summary>
    public object? Body { get; set; }

    /// <summary>
    /// Timeout in milliseconds for waiting/intercepting the request or response.
    /// </summary>
    public int? apiWaitTimeout;

    /// <summary>
    /// Whether the URL must be an exact match of the entire URL string (leading/trailing slashes are ignored) rather than a partial substring match when waiting/intercepting.
    /// Defaults to <c>false</c> if not explicitly provided.
    /// </summary>
    public bool? ExactUrlMatch { get; set; }
}
